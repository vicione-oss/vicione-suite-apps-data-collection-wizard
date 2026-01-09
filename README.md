# ViciOne Suite DataCollectionWizard

A ready-to-use template for installing a ViciOne module.

## Publish for ViciOne Suite

1. Build `DataCollectionWizard.Deployment` project
1. Execute `shared/deploy/publish-module.ps1 -module DataCollectionWizard -srcPath src -outputPath artifacts/publish`
1. Execute `shared/deploy/cleanup-module.ps1 -artifactPath artifacts/publish`
1. Zip the content of `artifacts\publish`

## VSE

To add a VSE to the cluster you need the IP-Address. Here's a [list](https://ifmworld.sharepoint.com/:x:/r/sites/ViciOne/Shared%20Documents/Infrastructure/Demowand%20TDL/Overview%20VSEs%20Demoboard%20TDL.xlsx?d=w1edecc3034414a698426b1a7b18e19e0&csf=1&web=1&e=cHpQ9x) of available VSE devices reachable within our network
