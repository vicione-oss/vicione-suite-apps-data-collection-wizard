using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Public;
using Sdk.Connections.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace DataCollectionWizard.Internal.Tests;

public class DeviceTreeBuilderTests
{
    /// <summary>
    /// Der DeviceTreeBuilder aktualisiert den bisher gespeicherten DeviceTree auf Basis folgender Informationen
    ///   - aktuellen IoT Core Gerätebaum
    ///   - aktuellen RemoteDevices
    ///   - aktuellen CloudDataAdapterConfigurations
    /// um einen neuen neuen DeviceTree zu erzeugen.
    ///
    /// Folgende Fälle müssen für die Devices geprüft werden:
    ///
    ///   alter DeviceTree | IoT Core Tree | neuer DeviceTree
    ///   -------------------------------------------------------------------------------------------------------------------
    ///          o         |        o      | IsNew = keine Änderung, IsOffline = false
    ///          o         |        x      | IsNew = keine Änderung, IsOffline = true
    ///          x         |        o      | IsNew = true          , IsOffline = false, neu dem Tree hinzufügen
    ///          !=        |        !=     | IsNew = true          , IsOffline = false, altes Gerät löschen, Neues hinzufügen
    ///          x         |        x      | keine Aktion
    ///
    ///   o/x = Ein Device ist (nicht) vorhanden. Wenn ein Device vorhanden ist, dann die *Id* stimmt überein.
    ///   !=  = Ein Device ist vorhanden, jedoch unterscheidet sich die *Id*.
    ///
    ///
    /// Folgende Fälle müssen für die MasterDevices geprüft werden:
    ///
    ///   Remote Devices | alter DeviceTree | IoT Core Tree | neuer DeviceTree
    ///   ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    ///         o        |       o          |        o      | IsNew = keine Änderung, IsOffline = false
    ///         o        |       o          |        x      | IsNew = keine Änderung, IsOffline = true
    ///         o        |       x          |        o      | IsNew = true          , IsOffline = false, neu dem Tree hinzufügen
    ///         o        |       x          |        x      | IsNew = true          , IsOffline = true , als Generic MasterDevice Dummy dem Tree hinzufügen
    ///         x        |       o          |        o      | aus dem Tree entfernen                   , (Zustand sollte nicht vorkommen, nur RemoteDevices werden im IoT Core mirrored)
    ///         x        |       o          |        x      | aus dem Tree entfernen
    ///         x        |       x          |        o      | keine Aktion                             , (Zustand sollte nicht vorkommen, nur RemoteDevices werden im IoT Core mirrored)
    ///         x        |       x          |        x      | keine Aktion
    /// 
    ///   o/x = Die *Id* (bei Vergleichen: alter Device Tree != IoT Core Tree) des MasterDevices bzw.
    ///         die *RemoteUri* (bei Vergleichen: Remote Devices != Device Tree/IoT Core Tree) ist (nicht) vorhanden.
    ///
    ///
    /// Folgende Fälle müssen für die Ports geprüft werden:
    ///
    ///   alter DeviceTree | IoT Core Tree | neuer DeviceTree
    ///   ---------------------------------------------------------------------------------------------------------------------------------
    ///          o         |        o      | IsNew = keine Änderung, IsOffline = false
    ///          o         |        x      | IsNew = keine Änderung, IsOffline = true , (Ports sollten aber theoretisch nicht verschwinden)
    ///          x         |        o      | IsNew = true          , IsOffline = false, neu dem Tree hinzufügen
    ///          x         |        x      | keine Aktion
    /// 
    ///   o/x = Die *Id* des Ports ist (nicht) vorhanden.
    /// </summary>
    public class ExtendCurrentDeviceTree
    {
        [Fact]
        public void RemovesCloudConfigurations()
        {
            var connectionId = Guid.Parse("3968155b-d9a0-4b6b-a81b-dc74fe3cbb4b");

            var currentTree = new DeviceTreeRoot()
            {
                Children =
                [
                    new DeviceTreeVseDevice
                    {
                        Children =
                        [
                            new DeviceTreeStructureNode
                            {
                                Children =
                                [
                                    new DeviceTreeVseRawData
                                    {
                                        Id = "TestVse/RawData/RawData1",
                                        Name =  "RawData1",
                                        RawDataConfigurations = new() {
                                            { connectionId, new RawDataSettings() }
                                        },
                                        SchedulerConfigurations =
                                        [
                                            new SchedulerConfiguration()
                                            {
                                                DataGroupIdentifier = connectionId,
                                            }
                                        ],
                                        Unit = "m/s²"
                                    }
                                ],
                                Id = "TestVse/RawData",
                                Name = "RawData",
                            },
                        ],
                        Id = "TestVse",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestVse",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeVseDevice
                {
                    Children =
                    [
                        new DeviceTreeStructureNode
                        {
                            Children =
                            [
                                new DeviceTreeVseRawData
                                {
                                    Id = "TestVse/RawData/RawData1",
                                    Name =  "RawData1",
                                    Unit = "m/s²",
                                }
                            ],
                            Id = "TestVse/RawData",
                            Name = "RawData",
                        },
                    ],
                    Id = "TestVse",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestVse",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(((DeviceTreeVseRawData)currentTree.Children[0].Children[0].Children[0]).SchedulerConfigurations);
            Assert.Empty(((DeviceTreeVseRawData)currentTree.Children[0].Children[0].Children[0]).RawDataConfigurations);
        }

        [Fact]
        public void AddsCloudConfigurations_ConfigurableRawData()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children =
                [
                    new DeviceTreeVseDevice
                    {
                        Children =
                        [
                            new DeviceTreeStructureNode
                            {
                                Children =
                                [
                                    new DeviceTreeVseRawData
                                    {
                                        Id = "TestVse/RawData/RawData1",
                                        Name =  "RawData1",
                                        Unit = "m/s²",
                                    }
                                ],
                                Id = "TestVse/RawData",
                                Name = "RawData",
                            },
                        ],
                        Id = "TestVse",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestVse",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeVseDevice
                {
                    Children =
                    [
                        new DeviceTreeStructureNode
                        {
                            Children =
                            [
                                new DeviceTreeVseRawData
                                {
                                    Id = "TestVse/RawData/RawData1",
                                    Name =  "RawData1",
                                    Unit = "m/s²",
                                }
                            ],
                            Id = "TestVse/RawData",
                            Name = "RawData",
                        },
                    ],
                    Id = "TestVse",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestVse",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            var connectionId = Guid.Parse("3968155b-d9a0-4b6b-a81b-dc74fe3cbb4b");

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, [new() { Id = connectionId, Name = "anna", Tags = [Constants.AnnaCloud], Type = ConnectionType.Http }]);

            Assert.Collection(((DeviceTreeVseRawData)currentTree.Children[0].Children[0].Children[0]).SchedulerConfigurations,
                e =>
                {
                    Assert.Equal(connectionId, e.DataGroupIdentifier);
                });
            Assert.Collection(((DeviceTreeVseRawData)currentTree.Children[0].Children[0].Children[0]).RawDataConfigurations,
                e =>
                {
                    Assert.Equal(connectionId, e.Key);
                    //Todo: sind 10 sek der richtige standard?
                    Assert.Equal(10000, e.Value.Duration);
                    Assert.Equal(100000, e.Value.Frequency);
                });
        }

        [Fact]
        public void AddsCloudConfigurations_SchedulableDataNode()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children =
                [
                    new DeviceTreeIoLinkMaster
                    {
                        Children =
                        [
                            new DeviceTreeIoLinkMasterPort
                            {
                                Children =
                                [
                                    new DeviceTreeDevice
                                    {
                                        Children =[
                                            new DeviceTreeBlobData()
                                            {
                                                Id = "TestDevice1/Blob",
                                                Name = "Blob",
                                            }
                                        ],
                                        Id = "TestDevice 1",
                                        IsOffline = true,
                                        Name =  "TestDevice 1",
                                    }
                                ],
                                Id = "TestPort 1",
                                Name = "TestPort",
                                SubIndex = 1,
                            },
                        ],
                        Id = "TestMaster",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestMaster",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Children =[
                                        new DeviceTreeBlobData()
                                        {
                                            Id = "TestDevice1/Blob",
                                            Name = "Blob",
                                        }
                                    ],
                                    Id = "TestDevice 1",
                                    IsOffline = true,
                                    Name =  "TestDevice 1",
                                }
                            ],
                            Id = "TestPort 1",
                            Name = "TestPort",
                            SubIndex = 1,
                        },
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            var connectionId = Guid.Parse("3968155b-d9a0-4b6b-a81b-dc74fe3cbb4b");

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, [new() { Id = connectionId, Name = "anna", Tags = [Constants.AnnaCloud], Type = ConnectionType.Http }]);

            Assert.Collection(((DeviceTreeBlobData)currentTree.Children[0].Children[0].Children[0].Children[0]).SchedulerConfigurations,
                e =>
                {
                    Assert.Equal(connectionId, e.DataGroupIdentifier);
                });
        }

        [Fact]
        public void AddsCloudConfigurations()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children =
                [
                    new DeviceTreeIoLinkMaster
                    {
                        Children =
                        [
                            new DeviceTreeIoLinkMasterPort
                            {
                                Children =
                                [
                                    new DeviceTreeDevice
                                    {
                                        Children =[
                                            new DeviceTreeProcessData()
                                            {
                                                Id = "TestDevice1/Data",
                                                Name = "Data",
                                            }
                                        ],
                                        Id = "TestDevice 1",
                                        IsOffline = true,
                                        Name =  "TestDevice 1",
                                    }
                                ],
                                Id = "TestPort 1",
                                Name = "TestPort",
                                SubIndex = 1,
                            },
                        ],
                        Id = "TestMaster",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestMaster",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Children =[
                                        new DeviceTreeProcessData()
                                        {
                                            Id = "TestDevice1/Data",
                                            Name = "Data",
                                        }
                                    ],
                                    Id = "TestDevice 1",
                                    IsOffline = true,
                                    Name =  "TestDevice 1",
                                }
                            ],
                            Id = "TestPort 1",
                            Name = "TestPort",
                            SubIndex = 1,
                        },
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            var connectionId = Guid.Parse("3968155b-d9a0-4b6b-a81b-dc74fe3cbb4b");

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, [new() { Id = connectionId, Name = "anna", Tags = [Constants.AnnaCloud], Type = ConnectionType.Http }]);

            Assert.Collection(((DeviceTreeProcessData)currentTree.Children[0].Children[0].Children[0].Children[0]).CompressorConfigurations,
                e =>
                {
                    Assert.Equal(connectionId, e.DataGroupIdentifier);
                });
        }

        [Fact]
        public void SetsDeviceStatusWhen_CurrentChanged()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Children =
                        [
                            new DeviceTreeDevice
                            {
                                Children =[
                                    new DeviceTreeProcessData()
                                    {
                                        Id = "TestDevice1/Data",
                                        IsOffline = true,
                                        Name = "Data",
                                    }
                                ],
                                Id = "TestDevice 1",
                                IsOffline = true,
                                Name =  "TestDevice 1",
                            }
                        ],
                        Id = "TestPort 1",
                        Name = "TestPort",
                        SubIndex = 1,
                    },
                    new DeviceTreeIoLinkMasterPort
                    {
                        Children =
                        [
                            new DeviceTreeDevice
                            {
                                Children =[
                                    new DeviceTreeProcessData()
                                    {
                                        Id = "TestDevice2/Data",
                                        IsOffline = true,
                                        Name = "Data",
                                    }
                                ],
                                Id = "TestDevice 2",
                                IsOffline = false,
                                Name =  "TestDevice 1",
                            }
                        ],
                        Id = "TestPort 2",
                        Name = "TestPort",
                        SubIndex = 2,
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Children =[
                                        new DeviceTreeProcessData()
                                        {
                                            Id = "TestDevice 1 changed/Data",
                                            Name = "Data",
                                        }
                                    ],
                                    Id = "TestDevice 1 changed",
                                    Name =  "TestDevice 1",
                                }
                            ],
                            Id = "TestPort 1",
                            Name = "TestPort",
                            SubIndex = 1,
                        },
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Children =[
                                        new DeviceTreeProcessData()
                                        {
                                            Id = "TestDevice 2 changed/Data",
                                            Name = "Data",
                                        }
                                    ],
                                    Id = "TestDevice 2 changed",

                                    Name =  "TestDevice 2",
                                }
                            ],
                            Id = "TestPort 2",
                            Name = "TestPort",
                            SubIndex = 2,
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };


            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Equal(2, currentTree.Children[0].Children.Count);

            Assert.Equal("TestDevice 1", currentTree.Children[0].Children[0].Children[0].Id);
            Assert.False(currentTree.Children[0].Children[0].Children[0].IsNew);
            Assert.True(currentTree.Children[0].Children[0].Children[0].IsOffline);

            Assert.Equal("TestDevice1/Data", currentTree.Children[0].Children[0].Children[0].Children[0].Id);
            Assert.False(currentTree.Children[0].Children[0].Children[0].Children[0].IsNew);
            Assert.True(currentTree.Children[0].Children[0].Children[0].Children[0].IsOffline);

            Assert.Equal("TestDevice 1 changed", currentTree.Children[0].Children[0].Children[1].Id);
            Assert.True(currentTree.Children[0].Children[0].Children[1].IsNew);
            Assert.False(currentTree.Children[0].Children[0].Children[1].IsOffline);

            Assert.Equal("TestDevice 1 changed/Data", currentTree.Children[0].Children[0].Children[1].Children[0].Id);
            Assert.True(currentTree.Children[0].Children[0].Children[1].Children[0].IsNew);
            Assert.False(currentTree.Children[0].Children[0].Children[1].Children[0].IsOffline);

            Assert.Equal("TestDevice 2", currentTree.Children[0].Children[1].Children[0].Id);
            Assert.False(currentTree.Children[0].Children[1].Children[0].IsNew);
            Assert.True(currentTree.Children[0].Children[1].Children[0].IsOffline);

            Assert.Equal("TestDevice2/Data", currentTree.Children[0].Children[1].Children[0].Children[0].Id);
            Assert.False(currentTree.Children[0].Children[1].Children[0].Children[0].IsNew);
            Assert.True(currentTree.Children[0].Children[1].Children[0].Children[0].IsOffline);

            Assert.Equal("TestDevice 2 changed", currentTree.Children[0].Children[1].Children[1].Id);
            Assert.True(currentTree.Children[0].Children[1].Children[1].IsNew);
            Assert.False(currentTree.Children[0].Children[1].Children[1].IsOffline);

            Assert.Equal("TestDevice 2 changed/Data", currentTree.Children[0].Children[1].Children[1].Children[0].Id);
            Assert.True(currentTree.Children[0].Children[1].Children[1].Children[0].IsNew);
            Assert.False(currentTree.Children[0].Children[1].Children[1].Children[0].IsOffline);
        }

        [Fact]
        public void SetsDeviceStatusWhen_CurrentFalse_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Id = "TestPort",
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(currentTree.Children[0].Children[0].Children);
        }

        [Fact]
        public void SetsDeviceStatusWhen_CurrentFalse_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Id = "TestPort",
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Id = "TestDevice",
                                    IsNew = true,
                                    IsOffline = false,
                                    Name =  "TestDevice 1",
                                }
                            ],
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children[0].Children);
            Assert.True(currentTree.Children[0].Children[0].Children[0].IsNew);
            Assert.False(currentTree.Children[0].Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsDeviceStatusWhen_CurrentTrue_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Children =
                        [
                            new DeviceTreeDevice
                            {
                                Id = "TestDevice",
                                IsOffline = false,
                                Name =  "TestDevice 1",
                            }
                        ],
                        Id = "TestPort",
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children[0].Children);
            Assert.True(currentTree.Children[0].Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsDeviceStatusWhen_CurrentTrue_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Children =
                        [
                            new DeviceTreeDevice
                            {
                                Id = "TestDevice",
                                IsOffline = true,
                                Name =  "TestDevice 1",
                            }
                        ],
                        Id = "TestPort",
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Id = "TestDevice",
                                    Name =  "TestDevice 1",
                                }
                            ],
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children[0].Children);
            Assert.False(currentTree.Children[0].Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsMasterDeviceStatusWhen_RemoteFalse_CurrentFalse_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            var iotCoreTree = new List<IDeviceTreeBase>();

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(currentTree.Children);
        }

        [Fact]
        public void SetsMasterDeviceStatusWhen_RemoteFalse_CurrentFalse_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(currentTree.Children);
        }

        [Fact]
        public void SetsMasterDeviceStatusWhen_RemoteFalse_CurrentTrue_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(currentTree.Children);
        }

        [Fact]
        public void SetsMasterDeviceStatusWhen_RemoteTrue_CurrentTrue_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster",
                IsOffline = false,
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>();

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children);
            Assert.True(currentTree.Children[0].IsOffline);
        }

        [Fact]
        public void SetsMasterDeviceStatusWhen_RemoteTrue_CurrentTrue_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster",
                IsOffline = true,
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children);
            Assert.False(currentTree.Children[0].IsOffline);
        }

        [Fact]
        public void SetsPortStatusWhen_CurrentFalse_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Empty(currentTree.Children[0].Children);
        }

        [Fact]
        public void SetsPortStatusWhen_CurrentFalse_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children);
            Assert.True(currentTree.Children[0].Children[0].IsNew);
            Assert.False(currentTree.Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsPortStatusWhen_CurrentTrue_IoTCoreFalse()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Id = "TestPort",
                        IsOffline = false,
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children);
            Assert.True(currentTree.Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsPortStatusWhen_CurrentTrue_IoTCoreTrue()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Children =
                [
                    new DeviceTreeIoLinkMasterPort
                    {
                        Id = "TestPort",
                        IsOffline = true,
                        Name = "TestPort",
                    }
                ],
                Id = "TestMaster",
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "TestMaster",
                Url = new Uri("http://127.0.0.1")
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Id = "TestPort",
                            Name = "TestPort",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Single(currentTree.Children[0].Children);
            Assert.False(currentTree.Children[0].Children[0].IsOffline);
        }

        [Fact]
        public void SetsUnknownStatus_False()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children = [
                    new DeviceTreeIoLinkMaster
                    {
                        Children =
                        [
                            new DeviceTreeIoLinkMasterPort()
                            {
                                Children =
                                [
                                    new DeviceTreeDevice()
                                    {
                                        Id = "TestMaster/Port1/Sensor",
                                        IsUnknown = true,
                                        Name = "Sensor",
                                    }
                                ],
                                Id = "TestMaster/Port1",
                                Name = "Port1"
                            }
                        ],
                        Id = "TestMaster",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestMaster",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort()
                        {
                            Children =
                            [
                                new DeviceTreeDevice()
                                {
                                    Children =
                                    [
                                        new DeviceTreeProcessData()
                                        {
                                            Id = "TestMaster/Port1/Sensor/ProcessData",
                                            Name = "ProcessData"
                                        }
                                    ],
                                    Id = "TestMaster/Port1/Sensor",
                                    Name = "Sensor"
                                }
                            ],
                            Id = "TestMaster/Port1",
                            Name = "Port1"
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.False(((DeviceTreeDevice)currentTree.Children[0].Children[0].Children[0]).IsUnknown);
            Assert.Collection(currentTree.Children[0].Children[0].Children[0].Children,
                e =>
                {
                    Assert.True(e.IsNew);
                });
        }

        [Fact]
        public void SetsUnknownStatus_True()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children = [
                    new DeviceTreeIoLinkMaster
                    {
                        Children =
                        [
                            new DeviceTreeIoLinkMasterPort()
                            {
                                Children =
                                [
                                    new DeviceTreeDevice()
                                    {
                                        Children =
                                        [
                                            new DeviceTreeProcessData()
                                            {
                                                Id = "TestMaster/Port1/Sensor/ProcessData",
                                                Name = "ProcessData"
                                            }
                                        ],
                                        Id = "TestMaster/Port1/Sensor",
                                        Name = "Sensor"
                                    }
                                ],
                                Id = "TestMaster/Port1",
                                Name = "Port1"
                            }
                        ],
                        Id = "TestMaster",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestMaster",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort()
                        {
                            Children =
                            [
                                new DeviceTreeDevice()
                                {
                                    Id = "TestMaster/Port1/Sensor",
                                    IsUnknown = true,
                                    Name = "Sensor",
                                }
                            ],
                            Id = "TestMaster/Port1",
                            Name = "Port1"
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.True(((DeviceTreeDevice)currentTree.Children[0].Children[0].Children[0]).IsUnknown);
            Assert.Collection(currentTree.Children[0].Children[0].Children[0].Children,
                e =>
                {
                    Assert.True(e.IsOffline);
                });
        }

        [Fact]
        public void UpdatesConstantNodes()
        {
            var currentTree = new DeviceTreeRoot()
            {
                Children = [
                    new DeviceTreeIoLinkMaster
                    {
                        Children =
                        [
                            new DeviceTreeConstantData()
                            {
                                Id = "TestMaster/Constant",
                                Name = "Constant",
                                Value = "old",
                            }
                        ],
                        Id = "TestMaster",
                        MacAddress = "ab:ab:ab:ab:ab",
                        Name = "TestMaster",
                        Url = new Uri("http://127.0.0.1")
                    }
                ]
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeConstantData()
                        {
                            Id = "TestMaster/Constant",
                            Name = "Constant",
                            Value = "new",
                        }
                    ],
                    Id = "TestMaster",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "TestMaster",
                    Url = new Uri("http://127.0.0.1")
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Equal("new", ((DeviceTreeConstantData)currentTree.Children[0].Children[0]).Value);
        }

        [Fact]
        public void UpdatesIoTCorePath()
        {
            var vse = new DeviceTreeVseDevice
            {
                Id = "deviceip",
                MacAddress = "ff:ff:ff:ff:ff",
                Name = "VseDevice",
                Url = new UriBuilder("deviceip").Uri,
            };

            var objects = new DeviceTreeStructureNode
            {
                Id = "deviceip/Objects",
                Name = "Objects",
            };

            var obj = new DeviceTreeVseObject
            {
                Alias = "RPM_LEFT",
                Id = "deviceip/Objects/Object01",
                InputId = "Input01",
                InputType = "External",
                Name = "Object01",
                ObjectId = "Object01",
                Path = "Device/Objects/Object01",
                Type = "re",
                Unit = string.Empty,
            };

            var child = new DeviceTreeProcessData
            {
                Id = "deviceip/Objects/Object01/Max",
                Name = "Max",
            };

            vse.Children.Add(objects);
            obj.Children.Add(child);
            objects.Children.Add(obj);

            var currentTree = new DeviceTreeRoot
            {
                Children =
                {
                    vse,
                }
            };

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeVseDevice
                {
                    Children =
                    {
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeVseObject
                                {
                                    Alias = "RPM_LEFT",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            Id = $"deviceip/Objects/Object01{DeviceTreeBuilder.IdSeparatorNameAlias}RPM_LEFT/Max",
                                            Name = "Max",
                                        }
                                    },
                                    Id = "deviceip/Objects/Object01",
                                    InputId = "Input01",
                                    InputType = "External",
                                    Name = "Object01",
                                    ObjectId = "Object01",
                                    Path = "127.0.0.1/Device/Objects/Object01",
                                    Type = "re",
                                    Unit = string.Empty
                                }
                            },
                            Id = "deviceip/Objects",
                            Name = "Objects"
                        }
                    },
                    Id = "deviceip",
                    MacAddress = "ff:ff:ff:ff:ff",
                    Name = "VseDevice",
                    Url = new UriBuilder("deviceip").Uri
                },
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Equal($"127.0.0.1/Device/Objects/Object01", obj.Path);
        }

        [Fact]
        public void UpdatesOnlineGenericMasterDevice()
        {
            var currentTree = new DeviceTreeRoot();
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster 1 Offline Random GUID",
                IsOffline = true,
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "IO-Link Master",
                Url = new Uri("http://127.0.0.1"),
            });
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster 2 Offline Random GUID",
                IsOffline = true,
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "IO-Link Master",
                Url = new Uri("http://127.0.0.2"),
            });
            currentTree.Children.Add(new DeviceTreeIoLinkMaster
            {
                Id = "TestMaster 3 Offline Random GUID",
                IsOffline = true,
                MacAddress = "ab:ab:ab:ab:ab",
                Name = "IO-Link Master",
                Url = new Uri("http://127.0.0.3"),
            });

            var iotCoreTree = new List<IDeviceTreeBase>
            {
                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Id = "TestMaster 1 Online IoTCore ID, TestMaster 1 Online Port 1, TestMaster 1 Online Port 1 Device",
                                    Name = "TestMaster 1 Online Port 1 Device",
                                }
                            ],
                            Id = "TestMaster 1 Online IoTCore ID, TestMaster 1 Online Port 1",
                            Name = "TestMaster 1 Online Port 1",
                        },
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Id = "TestMaster 1 Online IoTCore ID, TestMaster 1 Online Port 2, TestMaster 1 Online Port 2 Device",
                                    Name = "TestMaster 1 Online Port 2 Device",
                                }
                            ],
                            Id = "TestMaster 1 Online IoTCore ID, TestMaster 1 Online Port 2",
                            Name = "TestMaster 1 Online Port 2",
                        }
                    ],
                    Id = "TestMaster 1 Online IoTCore ID",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "IO-Link Master",
                    Url = new Uri("http://127.0.0.1"),
                },

                new DeviceTreeIoLinkMaster
                {
                    Children =
                    [
                        new DeviceTreeIoLinkMasterPort
                        {
                            Children =
                            [
                                new DeviceTreeDevice
                                {
                                    Id = "TestMaster 2 Online IoTCore ID, TestMaster 2 Online Port 1, TestMaster 2 Online Port 1 Device",
                                    Name = "TestMaster 2 Online Port 1 Device",
                                }
                            ],
                            Id = "TestMaster 2 Online IoTCore ID, TestMaster 2 Online Port 1",
                            Name = "TestMaster 2 Online Port 1",
                        }
                    ],
                    Id = "TestMaster 2 Online IoTCore ID",
                    MacAddress = "ab:ab:ab:ab:ab",
                    Name = "IO-Link Master",
                    Url = new Uri("http://127.0.0.2"),
                }
            };

            DeviceTreeBuilder.ExtendCurrentDeviceTree(currentTree, iotCoreTree, []);

            Assert.Equal(3, currentTree.Children.Count);

            var testMaster1 = currentTree.GetNodeAndDescendants().First(n => n.Id == "TestMaster 1 Online IoTCore ID");
            Assert.False(testMaster1.IsOffline);
            Assert.Equal(2, testMaster1.Children.Count);
            foreach (var port in testMaster1.Children)
                Assert.Single(port.Children);

            var testMaster2 = currentTree.GetNodeAndDescendants().First(n => n.Id == "TestMaster 2 Online IoTCore ID");
            Assert.False(testMaster2.IsOffline);
            Assert.Single(testMaster2.Children);
            Assert.Single(testMaster2.Children[0].Children);

            var testMaster3 = currentTree.GetNodeAndDescendants().First(n => n.Id == "TestMaster 3 Offline Random GUID");
            Assert.True(testMaster3.IsOffline);
            Assert.Empty(testMaster3.Children);
        }
    }
}
