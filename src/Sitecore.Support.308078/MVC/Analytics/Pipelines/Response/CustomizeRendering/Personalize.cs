using Sitecore;
using Sitecore.Analytics;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Analytics.Pipelines.RenderingRuleEvaluated;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;
using System.Collections.Generic;
using Sitecore.Mvc.Analytics.Pipelines.Response;
using Sitecore.Mvc.Analytics.Pipelines;
using Sitecore.Mvc.Analytics;

namespace Sitecore.Support.MVC.Analytics.Pipelines.Response.CustomizeRendering
{
  public class Personalize : CustomizeRenderingProcessor
  {
    public override void Process(CustomizeRenderingArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.IsCustomized && Tracker.IsActive)
      {
        Evaluate(args);
      }
    }

    protected virtual void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(context, "context");
      RenderingReference renderingReference = context.References.Find((RenderingReference r) => r.UniqueId == context.Reference.UniqueId);
      if (renderingReference == null)
      {
        args.Renderer = new EmptyRenderer();
      }
      else
      {
        ApplyChanges(args.Rendering, renderingReference);
      }
    }

    protected virtual void Evaluate(CustomizeRenderingArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Item item = args.PageContext.Item;
      if (item != null)
      {
        RenderingReference renderingReference = CustomizeRenderingProcessor.GetRenderingReference(args.Rendering, Context.Language, args.PageContext.Database);
        GetRenderingRulesArgs getRenderingRulesArgs = new GetRenderingRulesArgs(item, renderingReference);
        GetRenderingRulesPipeline.Run(getRenderingRulesArgs);
        RuleList<ConditionalRenderingsRuleContext> ruleList = getRenderingRulesArgs.RuleList;
        if (ruleList != null && ruleList.Count != 0)
        {
          List<RenderingReference> list = new List<RenderingReference>();
          list.Add(renderingReference);
          List<RenderingReference> references = list;
          ConditionalRenderingsRuleContext conditionalRenderingsRuleContext = new ConditionalRenderingsRuleContext(references, renderingReference);
          conditionalRenderingsRuleContext.Item = item;
          ConditionalRenderingsRuleContext context = conditionalRenderingsRuleContext;
          RunRules(ruleList, context);
          ApplyActions(args, context);
          args.IsCustomized = true;
        }
      }
    }

    protected virtual void RunRules(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
    {
      Assert.ArgumentNotNull(rules, "rules");
      Assert.ArgumentNotNull(context, "context");
      if (!RenderingRuleEvaluatedPipeline.IsEmpty())
      {
        rules.Evaluated += RulesEvaluatedHandler;
      }
      rules.RunFirstMatching(context);
    }

    private void RulesEvaluatedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, Rule<ConditionalRenderingsRuleContext> rule)
    {
      RenderingRuleEvaluatedArgs args = new RenderingRuleEvaluatedArgs(ruleList, ruleContext, rule);
      RenderingRuleEvaluatedPipeline.Run(args);
    }
  }
}