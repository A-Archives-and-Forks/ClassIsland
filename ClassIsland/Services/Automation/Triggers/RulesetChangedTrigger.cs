﻿using System;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using Material.Icons;

namespace ClassIsland.Services.Automation.Triggers;

[TriggerInfo("classisland.ruleSet.rulesetChanged", "规则集更新时", MaterialIconKind.TagMultipleOutline)]
public class RulesetChangedTrigger(IRulesetService rulesetService) : TriggerBase
{
    private IRulesetService RulesetService { get; } = rulesetService;

    public override void Loaded()
    {
        RulesetService.StatusUpdated += RulesetServiceOnStatusUpdated;
    }

    public override void UnLoaded()
    {
        RulesetService.StatusUpdated -= RulesetServiceOnStatusUpdated;
    }

    private void RulesetServiceOnStatusUpdated(object? sender, EventArgs e)
    {
        Trigger();
    }
}