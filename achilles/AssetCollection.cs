using HtmlAgilityPack;

namespace achilles {
    public class AssetCollection : List<Asset> {
        // TODO: performance profiling
        // surely there's a faster way to do these? idk

        // Quick Accessors
        public List<StylesheetAsset> Stylesheets { get => this.FindAll(a => a.Type == AssetType.Stylesheet).Cast<StylesheetAsset>().ToList(); }
        public List<ImageAsset> Images { get => this.FindAll(a => a.Type == AssetType.Image).Cast<ImageAsset>().ToList(); }
        public List<FormAsset> Forms { get => this.FindAll(a => a.Type == AssetType.Form).Cast<FormAsset>().ToList(); }
        public List<LinkAsset> Links { get => this.FindAll(a => a.Type == AssetType.Link).Cast<LinkAsset>().ToList(); }

        public static AssetCollection FromDocument(HtmlDocument htmlDocument) {
            AssetCollection assetCollection = new AssetCollection();
            foreach (HtmlNode node in htmlDocument.DocumentNode.Descendants()) {
                if (node.NodeType == HtmlNodeType.Element) {
                    switch (node.Name) {
                        case "style":
                            StylesheetAsset styl = new StylesheetAsset(node);
                            assetCollection.Add(styl);
                            break;
                        case "img":
                            ImageAsset img = new ImageAsset(node);
                            assetCollection.Add(img);
                            break;
                        case "form":
                            FormAsset frm = new FormAsset(node);
                            assetCollection.Add(frm);
                            break;
                        case "a":
                            LinkAsset a = new LinkAsset(node);
                            assetCollection.Add(a);
                            break;
                        default:
                            break;
                    }
                }
            }
            return assetCollection;
        }
    }
}
