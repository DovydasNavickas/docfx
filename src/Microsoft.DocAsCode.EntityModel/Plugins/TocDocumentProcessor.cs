﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Export(typeof(IDocumentProcessor))]
    public class TocDocumentProcessor : DisposableDocumentProcessor
    {
        public override string Name => nameof(TocDocumentProcessor);

        [ImportMany(nameof(TocDocumentProcessor))]
        public override IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }

        public override ProcessingPriority GetProcessingPriority(FileAndType file)
        {
            if (file.Type == DocumentType.Article)
            {
                if ("toc.md".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
                if ("toc.yml".Equals(Path.GetFileName(file.File), StringComparison.OrdinalIgnoreCase))
                {
                    return ProcessingPriority.High;
                }
            }
            return ProcessingPriority.NotSupportted;
        }

        public override FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
        {
            var filePath = Path.Combine(file.BaseDir, file.File);
            TocViewModel toc = LoadSingleToc(filePath);

            var repoDetail = GitUtility.GetGitDetail(filePath);

            // todo : metadata.
            return new FileModel(file, toc)
            {
                Uids = new[] { file.File }.ToImmutableArray(),
                LocalPathFromRepoRoot = repoDetail?.RelativePath
            };
        }

        public override SaveResult Save(FileModel model)
        {
            return new SaveResult
            {
                DocumentType = "Toc",
                ModelFile = model.File,
                TocMap = model.Properties.TocMap,
                LinkToFiles = model.Properties.LinkToFiles
            };
        }

        public override void UpdateHref(FileModel model, Func<string, string, string> updater)
        {
            if (updater == null) return;
            var toc = (TocViewModel)model.Content;
            var path = model.File;
            if (toc.Count > 0)
            {
                foreach (var item in toc)
                {
                    UpdateTocItemHref(item, path, updater);
                }
            }
        }

        private void UpdateTocItemHref(TocItemViewModel toc, string path, Func<string, string, string> updater)
        {
            toc.Href = updater(toc.Href, path);
            toc.OriginalHref = updater(toc.OriginalHref, path);
            if (toc.Items != null && toc.Items.Count > 0)
            {
                foreach (var item in toc.Items)
                {
                    UpdateTocItemHref(item, path, updater);
                }
            }
        }

        private TocViewModel LoadSingleToc(string filePath)
        {
            if ("toc.md".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return MarkdownTocReader.LoadToc(File.ReadAllText(filePath), filePath);
            }
            else if ("toc.yml".Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase))
            {
                return YamlUtility.Deserialize<TocViewModel>(filePath);
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file, supported toc files could be \"toc.md\" or \"toc.yml\".");
        }
    }
}
