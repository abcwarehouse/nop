using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.AbcSync.Models
{
    public class ImportModel
    {
        [NopResourceDisplayName(ImportPluginLocales.ArchiveTaskCcEmails)]
        public string ArchiveTaskCCEmails { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.CatalogUpdateFailureCcEmails)]
        public string CatalogUpdateFailureCCEmails { get; set; }

        [Required]
        [NopResourceDisplayName(ImportPluginLocales.RetryCount)]
        public int RetryCount { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.MainStoreWebTestSubscriptionId)]
        public string MainStoreWebTestSubscriptionId { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.MainStoreWebTestResourceGroup)]
        public string MainStoreWebTestResourceGroup { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.MainStoreWebTestName)]
        public string MainStoreWebTestName { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.ClearanceStoreWebTestSubscriptionId)]
        public string ClearanceStoreWebTestSubscriptionId { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.ClearanceStoreWebTestResourceGroup)]
        public string ClearanceStoreWebTestResourceGroup { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.ClearanceStoreWebTestName)]
        public string ClearanceStoreWebTestName { get; set; }

        [Required]
        [NopResourceDisplayName(ImportPluginLocales.StagingDbConnection)]
        public string StagingDbConnectionString { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.ImportABCStores)]
        public bool ImportABCStores { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.ImportHawthorneStores)]
        public bool ImportHawthorneStores { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipOldMattressesImport)]
        public bool SkipOldMattressesImport { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingProductsTask)]
        public bool SkipFillStagingProductsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingPricingTask)]
        public bool SkipFillStagingPricingTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingAccessoriesTask)]
        public bool SkipFillStagingAccessoriesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingBrandsTask)]
        public bool SkipFillStagingBrandsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingProductCategoryMappingsTask)]
        public bool SkipFillStagingProductCategoryMappingsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingScandownEndDatesTask)]
        public bool SkipFillStagingScandownEndDatesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingWarrantiesTask)]
        public bool SkipFillStagingWarrantiesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingRebatesTask)]
        public bool SkipFillStagingRebatesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingPromosTask)]
        public bool SkipFillStagingPromosTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipFillStagingRebateProductMappingsTask)]
        public bool SkipFillStagingRebateProductMappingsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportProductsTask)]
        public bool SkipImportProductsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipMapCategoriesTask)]
        public bool SkipMapCategoriesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportProductCategoryMappingsTask)]
        public bool SkipImportProductCategoryMappingsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipAddHomeDeliveryAttributesTask)]
        public bool SkipAddHomeDeliveryAttributesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportMarkdownsTask)]
        public bool SkipImportMarkdownsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportRelatedProductsTask)]
        public bool SkipImportRelatedProductsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportWarrantiesTask)]
        public bool SkipImportWarrantiesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipUnmapNonstockClearanceTask)]
        public bool SkipUnmapNonstockClearanceTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportRebatesTask)]
        public bool SkipImportRebatesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportPromosTask)]
        public bool SkipImportPromosTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipGenerateRebatePromoPageTask)]
        public bool SkipGenerateRebatePromoPageTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportDocumentsTask)]
        public bool SkipImportDocumentsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportIsamSpecsTask)]
        public bool SkipImportIsamSpecsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportFeaturedProductsTask)]
        public bool SkipImportFeaturedProductsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportProductFlagsTask)]
        public bool SkipImportProductFlagsTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportSotPicturesTask)]
        public bool SkipImportSotPicturesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipImportLocalPicturesTask)]
        public bool SkipImportLocalPicturesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipUnmapEmptyCategoriesTask)]
        public bool SkipUnmapEmptyCategoriesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipCleanDuplicateImagesTask)]
        public bool SkipCleanDuplicateImagesTask { get; set; }

        [NopResourceDisplayName(ImportPluginLocales.SkipSliExportTask)]
        public bool SkipSliExportTask { get; set; }
    }
}