namespace Sitecore.Support.Jobs
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Jobs;
    using Sitecore.Links;
    using Sitecore.Pipelines;
    using Sitecore.Pipelines.ReplaceItemReferences;
    using Sitecore.StringExtensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Internal link update in cloned subtree
    /// </summary>
    public class ReferenceReplacementJob : Sitecore.Jobs.ReferenceReplacementJob
    {
        private readonly Database database;

        private readonly Item sourceRoot;

        private readonly int sourceRootLength;

        private readonly Item copyRoot;

        private readonly bool deep;

        private readonly ID[] fieldIDs = Array.Empty<ID>();

        private Dictionary<ID, ID> mapOriginalAndClonedItems;

        /// <summary>
        /// Construct ReferenceReplacementJob object with source and copy item
        /// </summary>
        /// <param name="source">Item object</param>
        /// <param name="copy">Item object</param>
        public ReferenceReplacementJob(Item source, Item copy)
            : this(source, copy, deep: true, null)
        {
            Assert.ArgumentNotNull(source, "source");
            Assert.ArgumentNotNull(copy, "copy");
        }

        /// <summary>
        /// Construct ReferenceReplacementJob object with source and copy item
        /// </summary>
        /// <param name="source">Item object</param>
        /// <param name="copy">Item object</param>
        /// <param name="deep">If <c>true</c>, the job processes all descendant items; otherwise, the job processes only given item</param>
        /// <param name="fieldIDs">Array of field IDs to only replace references in; otherwise, all item fields are processed</param>
        public ReferenceReplacementJob(Item source, Item copy, bool deep, ID[] fieldIDs) : base(source, copy, deep, fieldIDs)
        {
            Assert.ArgumentNotNull(source, "source");
            Assert.ArgumentNotNull(copy, "copy");
            int length = source.Paths.FullPath.Length;
            Database database = source.Database;
            Assert.IsTrue(database == copy.Database, "items from different databases");
            this.database = database;
            sourceRoot = source;
            sourceRootLength = length;
            copyRoot = copy;
            this.deep = deep;
            this.fieldIDs = (fieldIDs ?? Array.Empty<ID>());
        }

        /// <summary>
        /// Construct ReferenceReplacementJob object with database, sourceid and copyid
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="sourceId">Source ID</param>
        /// <param name="copyId">Copy ID</param>
        public ReferenceReplacementJob(Database database, string sourceId, string copyId)
            : this(database, sourceId, copyId, deep: true, null)
        {
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(sourceId, "sourceId");
            Assert.ArgumentNotNull(copyId, "copyId");
        }

        /// <summary>
        /// Construct ReferenceReplacementJob object with database, sourceid and copyid
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="sourceId">Source ID</param>
        /// <param name="copyId">Copy ID</param>
        /// <param name="deep">If <c>true</c>, the job processes all descendant items; otherwise, the job processes only given item</param>
        /// <param name="fieldIDs">Array of field IDs to only replace references in; otherwise, all item fields are processed</param>
        public ReferenceReplacementJob(Database database, string sourceId, string copyId, bool deep, string[] fieldIDs) : base(database, sourceId, copyId, deep, fieldIDs)
        {
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(sourceId, "sourceId");
            Assert.ArgumentNotNull(copyId, "copyId");
            Item item = database.GetItem(sourceId);
            Assert.IsNotNull(item, "Source item {0} does not exist in {1} database".FormatWith(sourceId, database.Name));
            Item item2 = database.GetItem(copyId);
            Assert.IsNotNull(item2, "Target item {0} does not exist in {1} database".FormatWith(copyId, database.Name));
            int length = item.Paths.FullPath.Length;
            this.database = item.Database;
            sourceRoot = item;
            sourceRootLength = length;
            copyRoot = item2;
            this.deep = deep;
            this.fieldIDs = (from x in fieldIDs ?? Array.Empty<string>()
                             select ID.Parse(x)).ToArray();
        }

        /// <summary>
        /// Construct ReferenceReplacementJob object with source and copy item
        /// </summary>
        /// <param name="database">Database</param>
        /// <param name="sourceId">Source ID</param>
        /// <param name="copyId">Copy ID</param>
        public ReferenceReplacementJob(Database database, ID sourceId, ID copyId): base(database, sourceId, copyId)
        {
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(sourceId, "sourceId");
            Assert.ArgumentNotNull(copyId, "copyId");
            Item item = database.GetItem(sourceId);
            Assert.IsNotNull(item, "Source item {0} does not exist in {1} database".FormatWith(sourceId, database.Name));
            Item item2 = database.GetItem(copyId);
            Assert.IsNotNull(item2, "Target item {0} does not exist in {1} database".FormatWith(copyId, database.Name));
            int length = item.Paths.FullPath.Length;
            this.database = item.Database;
            sourceRoot = item;
            sourceRootLength = length;
            copyRoot = item2;
        }


        /// <summary>
        /// Pre-process cloned child
        /// </summary>
        /// <param name="copy">The copy item.</param>
        protected override void PreProcessClonedChild(Item copy)
        {
            if (mapOriginalAndClonedItems == null)
            {
                mapOriginalAndClonedItems = new Dictionary<ID, ID>();
            }
            if (copy.SourceUri != null)
            {
                Item item = Database.GetItem(copy.SourceUri);
                if (item != null)
                {
                    if (!mapOriginalAndClonedItems.ContainsKey(item.ID))
                    {
                        mapOriginalAndClonedItems.Add(item.ID, copy.ID);
                    }
                }
            }
            if (deep)
            {
                copy.GetChildren().ToList().ForEach(PreProcessClonedChild);
            }
        }

        /// <summary>
        /// Processes link
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="source">The source item.</param>
        /// <param name="copyVersion">The copy item.</param>
        protected override void ProcessLink(ItemLink link, Item source, Item copyVersion)
        {
            Assert.ArgumentNotNull(link, "link");
            Assert.ArgumentNotNull(source, "source");
            Assert.ArgumentNotNull(copyVersion, "copyVersion");
            Item targetItem = link.GetTargetItem();
            Assert.IsNotNull(targetItem, "linkTarget");
            string fullPath = targetItem.Paths.FullPath;
            string str = fullPath.Substring(sourceRootLength);
            string text = copyRoot.Paths.FullPath + str;
            Item item = null;
            if (mapOriginalAndClonedItems.ContainsKey(targetItem.ID))
            {
                item = database.GetItem(mapOriginalAndClonedItems[targetItem.ID]);
            }
            if (item == null)
            {
                Log.Warn("Cannot find corresponding item for {0} with path {1} in {2} database".FormatWith(source.Paths.FullPath, text, database.Name), this);
                return;
            }
            Field field = copyVersion.Fields[link.SourceFieldID];
            CustomField field2 = FieldTypeManager.GetField(field);
            Assert.IsNotNull(field2, "customField");
            field2.Relink(link, item);
        }
    }
}