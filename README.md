# ViciOne Suite DataCollectionWizard

A ready-to-use template for installing a ViciOne module.

## Publish for ViciOne Suite

1. Build `DataCollectionWizard.Deployment` project
1. Execute `shared/deploy/publish-module.ps1 -module DataCollectionWizard -srcPath src -outputPath artifacts/publish`
1. Execute `shared/deploy/cleanup-module.ps1 -artifactPath artifacts/publish`
1. Zip the content of `artifacts\publish`
