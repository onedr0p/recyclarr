using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.TestLibrary;
using Recyclarr.Json;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileConfigPhaseTest
{
    private static RadarrConfiguration SetupCfs(params CustomFormatConfig[] cfConfigs)
    {
        return NewConfig.Radarr() with
        {
            CustomFormats = cfConfigs
        };
    }

    [Test, AutoMockData]
    public void All_cfs_use_score_override(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 101, 1),
            NewCf.DataWithScore("", "id2", 201, 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile",
                    Score = 100
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
            {
                NewQp.Processed("test_profile", ("id1", 1, 100), ("id2", 2, 100))
            },
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test, AutoMockData]
    public void All_cfs_use_guide_scores_with_no_override(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 100, 1),
            NewCf.DataWithScore("", "id2", 200, 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
            {
                NewQp.Processed("test_profile", ("id1", 1, 100), ("id2", 2, 200))
            },
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test, AutoMockData]
    public void No_cfs_returned_when_no_score_in_guide_or_config(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.Data("", "id1", 1),
            NewCf.Data("", "id2", 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile"
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
            {
                NewQp.Processed("test_profile")
            },
            o => o.Excluding(x => x.ShouldCreate).Excluding(x => x.ScorelessCfs));
    }

    [Test, AutoMockData]
    public void Skip_duplicate_cfs_with_same_and_different_scores(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 100, 1)
        });

        var config = SetupCfs(
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"}
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile1", Score = 100}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile1", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile2", Score = 200}
                }
            },
            new CustomFormatConfig
            {
                TrashIds = new[] {"id1"},
                QualityProfiles = new List<QualityProfileScoreConfig>
                {
                    new() {Name = "test_profile2", Score = 100}
                }
            }
        );

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
            {
                NewQp.Processed("test_profile1", ("id1", 1, 100)),
                NewQp.Processed("test_profile2", ("id1", 1, 200))
            },
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test, AutoMockData]
    public void All_cfs_use_score_set(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScores("", "id1", 1, ("default", 101), ("set1", 102)),
            NewCf.DataWithScores("", "id2", 2, ("default", 201), ("set2", 202))
        });

        var config = NewConfig.Radarr() with
        {
            CustomFormats = new[]
            {
                new CustomFormatConfig
                {
                    TrashIds = new[] {"id1", "id2"},
                    QualityProfiles = new[]
                    {
                        new QualityProfileScoreConfig {Name = "test_profile"}
                    }
                }
            },
            QualityProfiles = new[]
            {
                new QualityProfileConfig
                {
                    Name = "test_profile",
                    ScoreSet = "set1"
                }
            }
        };

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
            {
                NewQp.Processed("test_profile", ("id1", 1, 102), ("id2", 2, 201)) with
                {
                    Profile = config.QualityProfiles.First()
                }
            },
            o => o.Excluding(x => x.ShouldCreate));
    }

    [Test, AutoMockData]
    public void Empty_trash_ids_list_is_ignored(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = Array.Empty<string>(),
            QualityProfiles = new List<QualityProfileScoreConfig>
            {
                new()
                {
                    Name = "test_profile",
                    Score = 100
                }
            }
        });

        var result = sut.Execute(config);

        result.Should().BeEmpty();
    }

    [Test, AutoMockData]
    public void Empty_quality_profiles_is_ignored(
        [Frozen] ProcessedCustomFormatCache cache,
        QualityProfileConfigPhase sut)
    {
        cache.AddCustomFormats(new[]
        {
            NewCf.DataWithScore("", "id1", 101, 1),
            NewCf.DataWithScore("", "id2", 201, 2)
        });

        var config = SetupCfs(new CustomFormatConfig
        {
            TrashIds = new[] {"id1", "id2"},
            QualityProfiles = Array.Empty<QualityProfileScoreConfig>()
        });

        var result = sut.Execute(config);

        result.Should().BeEmpty();
    }

    [Test]
    public void DemoNullableNotWorking()
    {
        var fs = new MockFileSystem();

        var jsonFile = fs.CurrentDirectory().File("testdata.json");
        fs.AddSameFileFromEmbeddedResource(jsonFile, typeof(QualityProfileConfigPhaseTest));
        jsonFile.Refresh();

        using var data = jsonFile.OpenRead();

        var unboxed = JsonSerializer.Deserialize<QualityProfileDto>(data, GlobalJsonSerializerSettings.Services);
        var boxed = JsonSerializer.Serialize(unboxed, GlobalJsonSerializerSettings.Services);
    }
}
