using AwesomeAssertions;
using Bunit;
using ClusterManagement.Public;
using ClusterManagement.Public.Iodds;
using DataCollectionWizard.Internal.Services;
using DataCollectionWizard.Internal.Services.DeviceDataflowGenerators;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sdk.Connections.Contracts;
using Sdk.Connections.Extensions;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DataCollectionWizard.Client.Tests.Vse;

public class VseGeneratorTests
{
    private const string FunctionBlockDirectory = "..\\..\\..\\..\\..\\drivers";
    private static readonly Guid s_annaDataGroupIdentifier = Guid.Parse("dee8ae99-f34d-486f-8f58-ace8b79c4a47");

    private readonly DeviceTreeVseDevice _vse = new()
    {
        Description = new DeviceTreeNodeDescription
        {
            Text = "127.0.0.1"
        },
        Id = "vse@127.0.0.1/",
        Url = new Uri("http://127.0.0.1"),
        MacAddress = "aa:bb:cc::ff",
        Name = "VSE100 - 00179322",
        NameAlias = "VSE100 - 00179322",
        Children =
        {
            new DeviceTreeStructureNode
            {
                Id = "vse@127.0.0.1//Alarms",
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
                                        IsWriteable = true,
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
                                Name = "Minimum",
                                DataType = DataType.Float32T,
                                Visible = true,
                            },
                            new DeviceTreeConstantData
                            {
                                DataType = DataType.StringT,
                                Id = "vse@127.0.0.1//Objects/Object01__!__EX01_UpperLimit_01/Name",
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
                            }
                        },
                        SchedulerConfigurations =
                        {
                            new()
                            {
                                DataGroupIdentifier = s_annaDataGroupIdentifier,
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
                            }
                        },
                        SchedulerConfigurations =
                        {
                            new()
                            {
                                DataGroupIdentifier = s_annaDataGroupIdentifier,
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
                                Enabled = true,
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
    };

    [Fact]
    [Trait(Traits.Category, Traits.System)]
    public void Builder_should_create_engine()
    {
        // Arrange
        using var ctx = new BunitContext();
        var instanceId = Guid.NewGuid();
        using var builder = ctx.SetupVseClusterBuilder(FunctionBlockDirectory, instanceId);

        // Act
        var application = builder.Cache.ClusterApplications.First();
        var vseDataflowName = "VSE Dataflow";
        var vseDataflow = builder.Cluster.Dataflows.FirstOrDefault(k => k.Name == vseDataflowName)
            ?? builder.Editors.Cluster.AddDataflow(vseDataflowName, new Version(0, 0, 1));

        var vseContainerName = "VSE Container";
        _ = vseDataflow.Root.Containers.FirstOrDefault(k => k.Name == vseContainerName)
            ?? builder.Editors.Container.AddContainer(vseDataflow.Root, vseContainerName, Guid.NewGuid(), new System.Drawing.Point(0, 0));

        var engineHostName = "VSE EngineHost";
        var engineHost = builder.Cache.EngineHosts.FirstOrDefault(k => k.Name == engineHostName)
            ?? builder.Editors.Application.AddEngineHost(application, engineHostName);

        var engineName = "VSE Engine";
        var engine = engineHost.Engines.FirstOrDefault(k => k.Name == engineName)
            ?? builder.Editors.EngineHost.AddEngine(engineHost, engineName);

        // example set engine propperties
        builder.Editors.Engine.SetRunIndex(engine, 5);

        // Assert
        engine.Should().NotBeNull();
        engine.RunIndex.Should().Be(5);
    }

    [Fact]
    [Trait(Traits.Category, Traits.System)]
    public void Builder_should_provide_function_block_designs()
    {
        // Arrange
        using var ctx = new BunitContext();
        var instanceId = Guid.NewGuid();
        using var builder = ctx.SetupVseClusterBuilder(FunctionBlockDirectory, instanceId);

        var fbNames = new List<string>
        {
            "VseObjectSubscriber",
            "VseCounterSubscriber",
            "VseAlarmSubscriber",
            "VseInputSubscriber"
        };

        // Act + Assert
        foreach (var fbName in fbNames)
        {
            builder.GetOrThrowFunctionBlockDesign(fbName);
        }
    }

    [Fact]
    [Trait(Traits.Category, Traits.System)]
    public void Builder_with_vse_support_can_be_created()
    {
        // Arrange
        using var ctx = new BunitContext();
        var instanceId = Guid.NewGuid();

        // Act
        using var builder = ctx.SetupVseClusterBuilder(FunctionBlockDirectory, instanceId);

        // Assert
        builder.Cache.FunctionBlockDesigns.Should().NotBeEmpty();
        builder.Cache.ClusterApplications.Should().HaveCount(1);
        builder.Cache.ClusterDependencies.Should().HaveCount(1);
    }

    [Fact]
    [Trait(Traits.Category, Traits.System)]
    public void Generates_basic_dataflow()
    {
        // Arrange
        using var ctx = new BunitContext();
        using var builder = ctx.SetupVseClusterBuilder(FunctionBlockDirectory, Guid.NewGuid());

        var ioddProvider = Substitute.For<IIoddStore>();
        ioddProvider.IoddDirectory.Returns("/iodds");

        var generator = new DataflowGenerator(builder, new NullLogger<DataflowGenerator>(), "mid", [new VseDataflowGenerator(new NullLogger<VseDataflowGenerator>())], [], []);
        var dataflow = builder.Cluster.Dataflows.First();
        // Act

        var annaConnection = new Connection
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
        };

        annaConnection.SetHttpConnection(new HttpConnection
        {
            BaseAddress = "http://anna",
            ApiKey = "testkey",
        });

        generator.Generate(_vse,
                           [annaConnection],
                           dataflow,
                           new(),
                           out _,
                           out _,
                           out _);

        ClusterSerializer.Serialize(builder.Cluster);
    }

    [Fact]
    [Trait(Traits.Category, Traits.System)]
    public void Generates_basic_empty_dataflow()
    {
        // Arrange
        using var ctx = new BunitContext();
        using var builder = ctx.SetupVseClusterBuilder(FunctionBlockDirectory, Guid.NewGuid());

        var ioddProvider = Substitute.For<IIoddStore>();
        ioddProvider.IoddDirectory.Returns("/iodds");

        var generator = new DataflowGenerator(builder, new NullLogger<DataflowGenerator>(), "mid", [new VseDataflowGenerator(new NullLogger<VseDataflowGenerator>())], [], []);
        var dataflow = builder.Cluster.Dataflows.First();
        // Act
        generator.Generate(new DeviceTreeVseDevice { Id = "id", MacAddress = "ab:cd:de:fe:dc", Name = "VSE", NameAlias = "VSE", Url = new Uri("http://10.45.24.101") },
                           [],
                           dataflow,
                           new(),
                           out _,
                           out _,
                           out _);

        ClusterSerializer.Serialize(builder.Cluster);
    }
}
