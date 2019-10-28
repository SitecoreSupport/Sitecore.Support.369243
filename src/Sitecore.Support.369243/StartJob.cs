namespace Sitecore.Support.Pipelines.ReplaceItemReferences
{
    using Sitecore.Diagnostics;
    using Sitecore.Jobs;
    using Sitecore.Pipelines.ReplaceItemReferences;

    /// <summary>
    ///   Starts reference replacement job.
    /// </summary>
    public class StartJob
    {
        /// <summary>
        /// Runs the processor.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Process(ReplaceItemReferencesArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.IsNotNull(args.SourceItem, "args.SourceItem");
            Assert.IsNotNull(args.CopyItem, "args.CopyItem");
            Sitecore.Support.Jobs.ReferenceReplacementJob referenceReplacementJob = new Sitecore.Support.Jobs.ReferenceReplacementJob(args.SourceItem, args.CopyItem, args.Deep, args.FieldIDs);
            if (args.Async)
            {
                referenceReplacementJob.StartAsync();
            }
            else
            {
                referenceReplacementJob.Start();
            }
        }
    }
}