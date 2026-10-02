using ViciOne.DeviceTree.Contracts;

namespace DataCollectionWizard.Backend.Factories;

internal static class FakeDeviceTreeFactory
{
    private static readonly Guid s_annaDataGroupIdentifier = Guid.Parse("dee8ae99-f34d-486f-8f58-ace8b79c4a47");
    private static readonly Guid s_anna2DataGroupIdentifier = Guid.Parse("850c34eb-0594-42f7-b7fb-e860cbf4d471");

    private static readonly DeviceTreeRoot s_deviceTree = new()
    {
        Children =
            {
                new DeviceTreeVseDevice
                {
                    Alias = "VSE100 - 00179322",
                    Children =
                    {
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeVseAlarm
                                {
                                    Alias = "OU02_Warning_02",
                                    Children =
                                    {
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Flag,
                                            Id = "vse@127.0.0.1//Alarms/Alarm02__!__OU02_Warning_02/Value",
                                            Name = "Value",
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Alarms//Alarm02__!__OU02_Warning_02",
                                    Name = "Alarm02",
                                    Path = "IoTCore-127.0.0.1/Device/Alarms/Alarm02",
                                    Type = "Warning"
                                },
                                new DeviceTreeVseAlarm
                                {
                                    Alias = "IO01_Damage_03",
                                    Children =
                                    {
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Flag,
                                            Id = "vse@127.0.0.1//Alarms//Alarm03__!__IO01_Damage_03/Value",
                                            Name = "Value",
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Alarms//Alarm03__!__IO01_Damage_03",
                                    Name = "Alarm03",
                                    Path = "IoTCore-127.0.0.1/Device/Alarms/Alarm03",
                                    Type = "Damage"
                                }
                            },
                            Id = "vse@127.0.0.1//Alarms",
                            IsNew = true,
                            Name = "Alarms"
                        },
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_01",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Limit",
                                            Name = "Limit",
                                        },
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/State",
                                            Name = "State",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Unit",
                                            Name = "Unit",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01/Value",
                                            Name = "Value",
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Counters/Counter01__!__OB01_ObjectState_01",
                                    Name = "Counter01",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter01",
                                    Type = "ObjectState",
                                    Unit = "Second"
                                },
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_02",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Limit",
                                            Name = "Limit",
                                        },
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/State",
                                            Name = "State",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Unit",
                                            Name = "Unit",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02/Value",
                                            Name = "Value",
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_02",
                                    Name = "Counter02",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter02",
                                    Type = "ObjectState",
                                    Unit = "Second"
                                },
                                new DeviceTreeVseCounter
                                {
                                    Alias = "OB01_ObjectState_03",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/Limit",
                                            Name = "Limit",
                                        },
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/State",
                                            Name = "State",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03/Unit",
                                            Name = "Unit",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                    Enabled = true,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Counters/Counter02__!__OB01_ObjectState_03/Value",
                                            Name = "Value",
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Counters/Counter03__!__OB01_ObjectState_03",
                                    Name = "Counter03",
                                    Path = "IoTCore-127.0.0.1/Device/Counters/Counter03",
                                    Type = "ObjectState",
                                    Unit = "Second"
                                }
                            },
                            Id  = "vse@127.0.0.1//Counters",
                            Name = "Counters",
                            Status = ConnectionStatus.Offline,
                        },
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeStructureNode
                                {
                                    Children =
                                    {
                                        new DeviceTreeVseInput
                                        {
                                            Alias = "External_01",
                                            Children =
                                            {
                                                new DeviceTreeAssignedName
                                                {
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
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Text,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01/Unit",
                                                    Name = "Unit",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Real,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01/Value",
                                                    Name = "Value",
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Inputs/External/Input01__!__External_01",
                                            InputId = 1,
                                            Name = "Input01",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input01",
                                            Unit = "°C"
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Alias = "External_02",
                                            Children =
                                            {
                                                new DeviceTreeAssignedName
                                                {
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
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Text,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02/Unit",
                                                    Name = "Unit",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Real,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02/Value",
                                                    Name = "Value",
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Inputs/External/Input02__!__External_02",
                                            InputId = 1,
                                            Name = "Input02",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input02",
                                            Unit = "°C"
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Alias = "External_03",
                                            Children =
                                            {
                                                new DeviceTreeAssignedName
                                                {
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
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Text,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03/Unit",
                                                    Name = "Unit",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Real,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03/Value",
                                                    Name = "Value",
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Inputs/External/Input03__!__External_03",
                                            InputId = 1,
                                            Name = "Input03",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input03",
                                            Unit = "°C"
                                        },
                                        new DeviceTreeVseInput
                                        {
                                            Alias = "External_04",
                                            Children =
                                            {
                                                new DeviceTreeAssignedName
                                                {
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
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Text,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04/Unit",
                                                    Name = "Unit",
                                                },
                                                new DeviceTreeProcessData
                                                {
                                                    CompressorConfigurations =
                                                    {
                                                        new CompressorConfiguration
                                                        {
                                                            Aggregation = AggregationFunction.MinMaxAvg,
                                                            CompressionTime = 10000,
                                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                        }
                                                    },
                                                    DataType = DataType.Real,
                                                    Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04/Value",
                                                    Name = "Value",
                                                }
                                            },
                                            Id = "vse@127.0.0.1//Inputs/External/Input04__!__External_04",
                                            InputId = 1,
                                            Name = "Input04",
                                            Path = "IoTCore-127.0.0.1/Device/Inputs/External/Input04",
                                            Unit = "°C"
                                        }
                                    },
                                    Id = "vse@127.0.0.1//Inputs/External",
                                    Name = "External"
                                }
                            },
                            Id = "vse@127.0.0.1//Inputs",
                            IsNew = true,
                            Name = "Inputs",
                            Status = ConnectionStatus.Offline
                        },
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeVseObject
                                {
                                    Alias = "EX01_UpperLimit_01",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Average",
                                            IsNew = true,
                                            Name = "Average",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/AveragingPeriod",
                                            IsNew = true,
                                            Name = "AveragingPeriod",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/BaseLine",
                                            IsNew = true,
                                            Name = "BaseLine",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Damage",
                                            IsNew = true,
                                            Name = "Damage",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Error",
                                            IsNew = true,
                                            Name = "Error",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Maximum",
                                            IsNew = true,
                                            Name = "Maximum",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Minimum",
                                            IsNew = true,
                                            Name = "Minimum",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeAssignedName
                                        {
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Name",
                                            Name = "Name",
                                            Status = ConnectionStatus.Offline,
                                            Value = "EX01_UpperLimit_01",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/RefValue",
                                            Name = "RefValue",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/RotSpeed",
                                            Name = "RotSpeed",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Unit",
                                            Name = "Unit",
                                            Status = ConnectionStatus.Offline,
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Warning",
                                            Name = "Warning",
                                        },
                                    },
                                    Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01",
                                    InputId = "1",
                                    InputType = "external",
                                    Name = "Object01",
                                    ObjectId = "1",
                                    Path = "IoTCore-127.0.0.1/Device/Objects/Object01",
                                    Type = "uppermonitor",
                                    Unit = "Achim"
                                },
                                new DeviceTreeVseObject
                                {
                                    Alias = "EX01_UpperLimit_02",
                                    Children =
                                    {
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Average",
                                            Name = "Average",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/AveragingPeriod",
                                            Name = "AveragingPeriod",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/BaseLine",
                                            Name = "BaseLine",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Damage",
                                            Name = "Damage",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Error",
                                            Name = "Error",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Maximum",
                                            Name = "Maximum",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Minimum",
                                            Name = "Minimum",
                                        },
                                        new DeviceTreeAssignedName
                                        {
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
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/RefValue",
                                            Name = "RefValue",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/RotSpeed",
                                            Name = "RotSpeed",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Text,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Unit",
                                            Name = "Unit",
                                        },
                                        new DeviceTreeProcessData
                                        {
                                            CompressorConfigurations =
                                            {
                                                new CompressorConfiguration
                                                {
                                                    Aggregation = AggregationFunction.MinMaxAvg,
                                                    CompressionTime = 10000,
                                                    DataGroupIdentifier = s_annaDataGroupIdentifier,
                                                }
                                            },
                                            DataType = DataType.Real,
                                            Id = "vse@127.0.0.1//Objects/Object02__!__EX02_UpperLimit_02/Warning",
                                            Name = "Warning",
                                        },
                                    },
                                    Id = "vse@127.0.0.1//Objects/Object02__!__EX01_UpperLimit_02",
                                    InputId = "2",
                                    InputType = "external",
                                    Name = "Object02",
                                    ObjectId = "2",
                                    Path = "IoTCore-127.0.0.1/Device/Objects/Object02",
                                    Type = "uppermonitor",
                                    Unit = "Achim"
                                }
                            },
                            Id = "vse@127.0.0.1//Objects",
                            Name = "Objects"
                        },
                        new DeviceTreeStructureNode
                        {
                            Children =
                            {
                                new DeviceTreeVseRawData
                                {
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
                                    Id = "vse@127.0.0.1//RawData/Sensor 1",
                                    Index = 1,
                                    Name = "Sensor 1",
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
                                },
                                new DeviceTreeVseRawData
                                {
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
                                    Id = "vse@127.0.0.1//RawData/Sensor 2",
                                    Index = 2,
                                    Name = "Sensor 2",
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
                                }
                            },
                            Id = "vse@127.0.0.1//RawData",
                            Name = "RawData"
                        },
                        new DeviceTreeVseVariants
                        {
                            Children =
                            {
                                new DeviceTreeProcessData
                                {
                                    CompressorConfigurations =
                                    {
                                        new CompressorConfiguration
                                        {
                                            Aggregation = AggregationFunction.MinMaxAvg,
                                            CompressionTime = 10000,
                                            DataGroupIdentifier = s_annaDataGroupIdentifier,
                                        }
                                    },
                                    DataType = DataType.Whole,
                                    Id = "vse@127.0.0.1//Variants/ActiveVariant",
                                    IsWriteable = true,
                                    Name = "ActiveVariant",
                                }
                            },
                            Id = "vse@127.0.0.1//Variants",
                            Name = "Variants",
                            Path = "IoTCore-127.0.0.1/Device/Variants"
                        }
                    },
                    Description = new DeviceTreeNodeDescription
                    {
                        Text = "127.0.0.1"
                    },
                    Id = "vse@127.0.0.1/",
                    MacAddress = "aa:bb:cc::ff",
                    Name = "VSE100 - 00179322",
                    Url = new UriBuilder("127.0.0.1").Uri
                }
            },
        Id = "root",
        Name = "Devices"
    };

    public static DeviceTreeRoot CreateDeviceTree()
        => s_deviceTree;
}
