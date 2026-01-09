using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Backend.Factories;

internal static class FakeDeviceTreeFactory
{
    private static readonly Guid s_annaDataGroupIdentifier = Guid.Parse("dee8ae99-f34d-486f-8f58-ace8b79c4a47");
    private static readonly Guid s_anna2DataGroupIdentifier = Guid.Parse("850c34eb-0594-42f7-b7fb-e860cbf4d471");

    private static readonly DeviceTreeRoot s_deviceTree = new()
    {
        Id = "root",
        Name = "Devices",
        Children =
            {
                new DeviceTreeVseDevice
                {
                    Description = new DeviceTreeNodeDescription
                    {
                        Text = "127.0.0.1"
                    },
                    Id = "vse@127.0.0.1/",
                    Url = new UriBuilder("127.0.0.1").Uri,
                    MacAddress = "aa:bb:cc::ff",
                    Name = "VSE100 - 00179322",
                    NameAlias = "VSE100 - 00179322",
                    Children =
                    {
                        new DeviceTreeStructureNode
                        {
                            Id = "vse@127.0.0.1//Alarms",
                            IsNew = true,
                            Name = "Alarms",
                            Children =
                            {
                                new DeviceTreeVseAlarm
                                {
                                    Alias = "OU02_Warning_02",
                                    Id = "vse@127.0.0.1//Alarms//Alarm02__!__OU02_Warning_02",
                                    Name = "Alarm02",
                                    Path = "IoTCore-127.0.0.1/Device/Alarms/Alarm02",
                                    Type = "Warning",
                                    Children =
                                    {
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Alarms/Alarm02__!__OU02_Warning_02/Name",
                                            Name = "Name",
                                            Value = "OU02_Warning_02",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            DataType = DataType.BooleanT,
                                            Id = "vse@127.0.0.1//Alarms/Alarm02__!__OU02_Warning_02/Value",
                                            Name = "Value",
                                            Visible = true,
                                        }
                                    }
                                },
                                new DeviceTreeVseAlarm
                                {
                                    Alias = "IO01_Damage_03",
                                    Id = "vse@127.0.0.1//Alarms//Alarm03__!__IO01_Damage_03",
                                    Name = "Alarm03",
                                    Path = "IoTCore-127.0.0.1/Device/Alarms/Alarm03",
                                    Type = "Damage",
                                    Children =
                                    {
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Alarms//Alarm03__!__IO01_Damage_03/Name",
                                            Name = "Name",
                                            Value = "IO01_Damage_03",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.BooleanT,
                                            Id = "vse@127.0.0.1//Alarms//Alarm03__!__IO01_Damage_03/Value",
                                            Name = "Value",
                                            Visible = true,
                                        }
                                    }
                                }
                            }
                        },
                        new DeviceTreeStructureNode
                        {
                            Id  = "vse@127.0.0.1//Counters",
                            IsOffline = true,
                            Name = "Counters",
                            Children =
                            {
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_01",
                                    Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01",
                                    Name = "Counter01",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter01",
                                    Type = "ObjectState",
                                    Unit = "Second",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Limit",
                                            Name = "Limit",
                                            Visible = true,
                                        },
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Name",
                                            Name = "Name",
                                            Value = "OB01_ObjectState_01",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/State",
                                            Name = "State",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Unit",
                                            Name = "Unit",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Value",
                                            Name = "Value",
                                            Visible = true,
                                        }
                                    }
                                },
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_02",
                                    Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02",
                                    Name = "Counter02",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter02",
                                    Type = "ObjectState",
                                    Unit = "Second",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Limit",
                                            Name = "Limit",
                                            Visible = true,
                                        },
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Name",
                                            Name = "Name",
                                            Value = "OB01_ObjectState_02",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/State",
                                            Name = "State",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Unit",
                                            Name = "Unit",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Value",
                                            Name = "Value",
                                            Visible = true,
                                        }
                                    }
                                },
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_03",
                                    Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03",
                                    Name = "Counter03",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter03",
                                    Type = "ObjectState",
                                    Unit = "Second",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/Limit",
                                            Name = "Limit",
                                            Visible = true,
                                        },
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/Name",
                                            Name = "Name",
                                            Value = "OB01_ObjectState_03",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/State",
                                            Name = "State",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/Unit",
                                            Name = "Unit",
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Float32T,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Value",
                                            Name = "Value",
                                            Visible = true,
                                        }
                                    }
                                }
                            },
                        },
                        new DeviceTreeStructureNode
                        {
                            Id = "vse@127.0.0.1//Inputs",
                            IsOffline = true,
                            IsNew = true,
                            Name = "Inputs",
                            Children =
                            {
                                new DeviceTreeStructureNode
                                {
                                    Id = "vse@127.0.0.1//Inputs/External",
                                    Name = "External",
                                    Children =
                                    {
                                        new DeviceTreeVseInput
                                        {
                                            Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01",
                                            InputId = 1,
                                            Name = "Input01",
                                            Alias = "External_01",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input01",
                                            Unit = "°C",
                                            Children =
                                            {
                                                new DeviceTreeConstantData
                                                {
                                                    DataType = DataType.StringT,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01/Name",
                                                    Name = "Name",
                                                    Value = "External_01",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01/Unit",
                                                    Name = "Unit",
                                                    DataType = DataType.StringT,
                                                    Visible = true,
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01/Value",
                                                    Name = "Value",
                                                    DataType = DataType.Float32T,
                                                    Visible = true,
                                                }
                                            }
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02",
                                            InputId = 1,
                                            Name = "Input02",
                                            Alias = "External_02",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input02",
                                            Unit = "°C",
                                            Children =
                                            {
                                                new DeviceTreeConstantData
                                                {
                                                    DataType = DataType.StringT,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02/Name",
                                                    Name = "Name",
                                                    Value = "External_02",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02/Unit",
                                                    Name = "Unit",
                                                    DataType = DataType.StringT,
                                                    Visible = true,
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                        CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02/Value",
                                                    Name = "Value",
                                                    DataType = DataType.Float32T,
                                                    Visible = true,
                                                }
                                            }
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03",
                                            InputId = 1,
                                            Name = "Input03",
                                            Alias = "External_03",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input03",
                                            Unit = "°C",
                                            Children =
                                            {
                                                new DeviceTreeConstantData
                                                {
                                                    DataType = DataType.StringT,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03/Name",
                                                    Name = "Name",
                                                    Value = "External_03",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03/Unit",
                                                    Name = "Unit",
                                                    DataType = DataType.StringT,
                                                    Visible = true,
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03/Value",
                                                    Name = "Value",
                                                    DataType = DataType.Float32T,
                                                    Visible = true,
                                                }
                                            }
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04",
                                            InputId = 1,
                                            Name = "Input04",
                                            Alias = "External_04",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input04",
                                            Unit = "°C",
                                            Children =
                                            {
                                                new DeviceTreeConstantData
                                                {
                                                    DataType = DataType.StringT,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04/Name",
                                                    Name = "Name",
                                                    Value = "External_04",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04/Unit",
                                                    Name = "Unit",
                                                    DataType = DataType.StringT,
                                                    Visible = true,
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                            PoolingMode = PoolingMode.MinMaxAvg,
                                                        }
                                                    },
                                                    Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04/Value",
                                                    Name = "Value",
                                                    DataType = DataType.Float32T,
                                                    Visible = true,
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new DeviceTreeStructureNode
                        {
                            Id = "vse@127.0.0.1//Objects",
                            Name = "Objects",
                            Children =
                            {
                                new DeviceTreeVseObject
                                {
                                    Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01",
                                    Name = "Object01",
                                    Alias = "EX01_UpperLimit_01",
                                    ObjectId = "1",
                                    InputId = "1",
                                    InputType = "external",
                                    Path = "IoTCore-127.0.0.1/Device/Objects/Object01",
                                    Type = "uppermonitor",
                                    Unit = "Achim",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Average",
                                            IsNew = true,
                                            Name = "Average",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/AveragingPeriod",
                                            IsNew = true,
                                            Name = "AveragingPeriod",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/BaseLine",
                                            IsNew = true,
                                            Name = "BaseLine",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Damage",
                                            IsNew = true,
                                            IsOffline = true,
                                            Name = "Damage",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Error",
                                            IsNew = true,
                                            IsOffline = true,
                                            Name = "Error",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Maximum",
                                            IsNew = true,
                                            IsOffline = true,
                                            Name = "Maximum",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Minimum",
                                            IsNew = true,
                                            IsOffline = true,
                                            Name = "Minimum",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Name",
                                            IsOffline = true,
                                            Name = "Name",
                                            Value = "EX01_UpperLimit_01",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/RefValue",
                                            IsOffline = true,
                                            Name = "RefValue",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/RotSpeed",
                                            IsOffline = true,
                                            Name = "RotSpeed",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Unit",
                                            IsOffline = true,
                                            Name = "Unit",
                                            DataType = DataType.StringT,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Warning",
                                            Name = "Warning",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                    }
                                },
                                new DeviceTreeVseObject
                                {
                                    Id = "vse@127.0.0.1//Objects/Object02__!__EX01_UpperLimit_02",
                                    Name = "Object02",
                                    Alias = "EX01_UpperLimit_02",
                                    ObjectId = "2",
                                    InputId = "2",
                                    InputType = "external",
                                    Path = "IoTCore-127.0.0.1/Device/Objects/Object02",
                                    Type = "uppermonitor",
                                    Unit = "Achim",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Average",
                                            Name = "Average",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/AveragingPeriod",
                                            Name = "AveragingPeriod",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/BaseLine",
                                            Name = "BaseLine",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Damage",
                                            Name = "Damage",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Error",
                                            Name = "Error",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Maximum",
                                            Name = "Maximum",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Minimum",
                                            Name = "Minimum",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeConstantData
                                        {
                                            DataType = DataType.StringT,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Name",
                                            Name = "Name",
                                            Value = "EX02_UpperLimit_02",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/RefValue",
                                            Name = "RefValue",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/RotSpeed",
                                            Name = "RotSpeed",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Unit",
                                            Name = "Unit",
                                            DataType = DataType.StringT,
                                            Visible = true,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    PoolingMode = PoolingMode.MinMaxAvg,
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Warning",
                                            Name = "Warning",
                                            DataType = DataType.Float32T,
                                            Visible = true,
                                        },
                                    }
                                }
                            }
                        },
                        new DeviceTreeStructureNode
                        {
                            Id = "vse@127.0.0.1//RawData",
                            Name = "RawData",
                            Children =
                            {
                                new DeviceTreeVseRawData
                                {
                                    Id = "vse@127.0.0.1//RawData/Sensor 1",
                                    Index = 1,
                                    Name = "Sensor 1",
                                    Unit = "m/s²",
                                    RawDataConfigurations =
                                    {
                                        { s_annaDataGroupIdentifier, new RawDataSettings
                                            {
                                                Duration = 4000,
                                                Frequency = 100000,
                                            }
                                        },
                                        { s_anna2DataGroupIdentifier, new RawDataSettings
                                            {
                                                Duration = 4000,
                                                Frequency = 100000,
                                            }
                                        },
                                    },
                                    SchedulerConfigurations =
                                    {
                                        new()
                                        {
                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                            Times = { { DayOfWeek.Monday, [TimeSpan.FromSeconds(0)] }  },
                                        },
                                        new()
                                        {
                                            DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                            Times = { { DayOfWeek.Monday, [TimeSpan.FromSeconds(0)] }  },
                                        }
                                    },
                                    EventTriggerConfigurations =
                                    {
                                        new()
                                        {
                                            IsSensorConfigured = true,
                                            Name = "EX01_UpperLimit_01",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                        new()
                                        {
                                            IsSensorConfigured = true,
                                            Name = "EX02_UpperLimit_02",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object02__!__EX01_UpperLimit_02",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                    Enabled = true,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                        new()
                                        {
                                            Name = "EX03_UpperLimit_03",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object03__!__EX01_UpperLimit_03",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                    },
                                },
                                new DeviceTreeVseRawData
                                {
                                    Id = "vse@127.0.0.1//RawData/Sensor 2",
                                    Index = 2,
                                    Name = "Sensor 2",
                                    Unit = "m/s²",
                                    RawDataConfigurations =
                                    {
                                        { s_annaDataGroupIdentifier, new RawDataSettings
                                            {
                                                Duration = 4000,
                                                Frequency = 100000,
                                            }
                                        },
                                        { s_anna2DataGroupIdentifier, new RawDataSettings
                                            {
                                                Duration = 4000,
                                                Frequency = 100000,
                                            }
                                        },
                                    },
                                    SchedulerConfigurations =
                                    {
                                        new()
                                        {
                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                            Times = { { DayOfWeek.Monday, [TimeSpan.FromSeconds(0)] }  },
                                        },
                                        new()
                                        {
                                            DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                            Times = { { DayOfWeek.Monday, [TimeSpan.FromSeconds(0)] }  },
                                        }
                                    },
                                    EventTriggerConfigurations =
                                    {
                                        new()
                                        {
                                            Name = "EX01_UpperLimit_01",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                        new()
                                        {
                                            Name = "EX02_UpperLimit_02",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object02__!__EX01_UpperLimit_02",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                    Enabled = true,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                        new()
                                        {
                                            IsSensorConfigured = true,
                                            Name = "EX03_UpperLimit_03",
                                            ReferenceNodeId = "vse@127.0.0.1//Objects/Object03__!__EX01_UpperLimit_03",
                                            Triggers =
                                            {
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Delay = 1,
                                                },
                                                new ()
                                                {
                                                    DataGroupIdentifier = s_anna2DataGroupIdentifier,
                                                    Delay = 1,
                                                }
                                            }
                                        },
                                    },
                                }
                            }
                        },
                        new DeviceTreeVseVariants
                        {
                            Id = "vse@127.0.0.1//Variants",
                            Name = "Variants",
                            Path = "IoTCore-127.0.0.1/Device/Variants",
                            Children =
                            {
                                new DeviceTreeProcessData
                                {
                                    CompressorConfigurations =
                                    {
                                        new CompressorConfiguration
                                        {
                                            CompressionTime = 10000,
                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                            PoolingMode = PoolingMode.MinMaxAvg,
                                        }
                                    },
                                    DataType = DataType.IntegerT,
                                    Id = "vse@127.0.0.1//Variants/ActiveVariant",
                                    IsWriteable = true,
                                    Name = "ActiveVariant",
                                    Visible = true,
                                }
                            }
                        }
                    }
                }
            }
    };

    public static DeviceTreeRoot CreateDeviceTree()
        => s_deviceTree;
}
