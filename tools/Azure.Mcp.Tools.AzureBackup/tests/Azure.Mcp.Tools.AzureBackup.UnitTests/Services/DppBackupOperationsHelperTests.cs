// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Mcp.Tools.AzureBackup.Services;
using Xunit;

namespace Azure.Mcp.Tools.AzureBackup.UnitTests.Services;

public class DppBackupOperationsHelperTests
{
    #region MapWorkloadTypeToArmResourceType

    [Theory]
    [InlineData("azuredisk", "Microsoft.Compute/disks")]
    [InlineData("AzureDisk", "Microsoft.Compute/disks")]
    [InlineData("AZUREDISK", "Microsoft.Compute/disks")]
    [InlineData("azureblob", "Microsoft.Storage/storageAccounts/blobServices")]
    [InlineData("postgresqlflexible", "Microsoft.DBforPostgreSQL/flexibleServers")]
    [InlineData("mysqlflexible", "Microsoft.DBforMySQL/flexibleServers")]
    [InlineData("aks", "Microsoft.ContainerService/managedClusters")]
    [InlineData("AKS", "Microsoft.ContainerService/managedClusters")]
    [InlineData("elasticsan", "Microsoft.ElasticSan/elasticSans/volumeGroups")]
    [InlineData("ElasticSan", "Microsoft.ElasticSan/elasticSans/volumeGroups")]
    public void MapWorkloadTypeToArmResourceType_ReturnsMappedType(string workloadType, string expected)
    {
        var result = DppBackupOperations.MapWorkloadTypeToArmResourceType(workloadType);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Microsoft.Compute/disks")]
    [InlineData("Microsoft.ContainerService/managedClusters")]
    [InlineData("some.custom/resourceType")]
    public void MapWorkloadTypeToArmResourceType_PassesThroughUnknownTypes(string workloadType)
    {
        var result = DppBackupOperations.MapWorkloadTypeToArmResourceType(workloadType);
        Assert.Equal(workloadType, result);
    }

    #endregion

    #region UsesOperationalStore

    [Theory]
    [InlineData("Microsoft.Compute/disks", true)]
    [InlineData("microsoft.compute/disks", true)]
    [InlineData("Microsoft.Storage/storageAccounts/blobServices", true)]
    [InlineData("Microsoft.ContainerService/managedClusters", true)]
    [InlineData("microsoft.containerservice/managedclusters", true)]
    [InlineData("Microsoft.ElasticSan/elasticSans/volumeGroups", true)]
    [InlineData("azuredisk", true)]
    [InlineData("azureblob", true)]
    [InlineData("aks", true)]
    [InlineData("elasticsan", true)]
    [InlineData("Microsoft.DBforPostgreSQL/flexibleServers", false)]
    [InlineData("Microsoft.DBforMySQL/flexibleServers", false)]
    [InlineData("unknown", false)]
    public void UsesOperationalStore_ReturnsExpected(string datasourceType, bool expected)
    {
        var result = DppBackupOperations.UsesOperationalStore(datasourceType);
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsBlobOperationalBackup

    [Theory]
    [InlineData("Microsoft.Storage/storageAccounts/blobServices", true)]
    [InlineData("microsoft.storage/storageaccounts/blobservices", true)]
    [InlineData("azureblob", true)]
    [InlineData("AzureBlob", true)]
    [InlineData("Microsoft.Compute/disks", false)]
    [InlineData("Microsoft.ContainerService/managedClusters", false)]
    [InlineData("aks", false)]
    [InlineData("elasticsan", false)]
    public void IsBlobOperationalBackup_ReturnsExpected(string datasourceType, bool expected)
    {
        var result = DppBackupOperations.IsBlobOperationalBackup(datasourceType);
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsElasticSanWorkload

    [Theory]
    [InlineData("Microsoft.ElasticSan/elasticSans/volumeGroups", true)]
    [InlineData("microsoft.elasticsan/elasticsans/volumegroups", true)]
    [InlineData("elasticsan", true)]
    [InlineData("ElasticSan", true)]
    [InlineData("ELASTICSAN", true)]
    [InlineData("Microsoft.Compute/disks", false)]
    [InlineData("Microsoft.ContainerService/managedClusters", false)]
    [InlineData("aks", false)]
    [InlineData("azureblob", false)]
    public void IsElasticSanWorkload_ReturnsExpected(string datasourceType, bool expected)
    {
        var result = DppBackupOperations.IsElasticSanWorkload(datasourceType);
        Assert.Equal(expected, result);
    }

    #endregion

    #region IsAksWorkload

    [Theory]
    [InlineData("Microsoft.ContainerService/managedClusters", true)]
    [InlineData("microsoft.containerservice/managedclusters", true)]
    [InlineData("aks", true)]
    [InlineData("AKS", true)]
    [InlineData("Aks", true)]
    [InlineData("Microsoft.Compute/disks", false)]
    [InlineData("Microsoft.ElasticSan/elasticSans/volumeGroups", false)]
    [InlineData("elasticsan", false)]
    [InlineData("azureblob", false)]
    [InlineData("azuredisk", false)]
    public void IsAksWorkload_ReturnsExpected(string datasourceType, bool expected)
    {
        var result = DppBackupOperations.IsAksWorkload(datasourceType);
        Assert.Equal(expected, result);
    }

    #endregion

    #region RequiresDataSourceSetInfo

    [Theory]
    [InlineData("Microsoft.ElasticSan/elasticSans/volumeGroups", true)]
    [InlineData("elasticsan", true)]
    [InlineData("Microsoft.ContainerService/managedClusters", true)]
    [InlineData("aks", true)]
    [InlineData("Microsoft.Compute/disks", false)]
    [InlineData("azuredisk", false)]
    [InlineData("azureblob", false)]
    [InlineData("Microsoft.Storage/storageAccounts/blobServices", false)]
    [InlineData("Microsoft.DBforPostgreSQL/flexibleServers", false)]
    [InlineData("mysqlflexible", false)]
    public void RequiresDataSourceSetInfo_ReturnsExpected(string datasourceType, bool expected)
    {
        var result = DppBackupOperations.RequiresDataSourceSetInfo(datasourceType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RequiresDataSourceSetInfo_IncludesBothEsanAndAks()
    {
        // Verify that both ESAN and AKS workloads require DataSourceSetInfo
        Assert.True(DppBackupOperations.RequiresDataSourceSetInfo("elasticsan"));
        Assert.True(DppBackupOperations.RequiresDataSourceSetInfo("aks"));
        Assert.True(DppBackupOperations.RequiresDataSourceSetInfo("Microsoft.ElasticSan/elasticSans/volumeGroups"));
        Assert.True(DppBackupOperations.RequiresDataSourceSetInfo("Microsoft.ContainerService/managedClusters"));

        // Verify other workloads don't require it
        Assert.False(DppBackupOperations.RequiresDataSourceSetInfo("azuredisk"));
        Assert.False(DppBackupOperations.RequiresDataSourceSetInfo("azureblob"));
        Assert.False(DppBackupOperations.RequiresDataSourceSetInfo("postgresqlflexible"));
    }

    #endregion

    #region GetElasticSanParentId

    [Fact]
    public void GetElasticSanParentId_ExtractsParentFromVolumeGroupId()
    {
        var volumeGroupId = new ResourceIdentifier(
            "/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg1/providers/Microsoft.ElasticSan/elasticSans/mysan/volumeGroups/myvg");

        var parentId = DppBackupOperations.GetElasticSanParentId(volumeGroupId);

        Assert.Equal(
            "/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg1/providers/Microsoft.ElasticSan/elasticSans/mysan",
            parentId.ToString());
    }

    [Fact]
    public void GetElasticSanParentId_HandlesVaryingCase()
    {
        // The comparison is case-insensitive for /volumeGroups/
        var volumeGroupId = new ResourceIdentifier(
            "/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg1/providers/Microsoft.ElasticSan/elasticSans/testsan/VolumeGroups/testvg");

        var parentId = DppBackupOperations.GetElasticSanParentId(volumeGroupId);

        Assert.Equal(
            "/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg1/providers/Microsoft.ElasticSan/elasticSans/testsan",
            parentId.ToString());
    }

    [Fact]
    public void GetElasticSanParentId_ReturnsOriginalForNonVolumeGroup()
    {
        // If the ID doesn't contain /volumeGroups/, falls back to Parent or original
        var diskId = new ResourceIdentifier(
            "/subscriptions/00000000-0000-0000-0000-000000000001/resourceGroups/rg1/providers/Microsoft.Compute/disks/mydisk");

        var parentId = DppBackupOperations.GetElasticSanParentId(diskId);

        // Should return the parent or original ID since there's no /volumeGroups/ segment
        Assert.NotNull(parentId);
    }

    #endregion
}
