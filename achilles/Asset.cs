using System;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace achilles {
    public enum AssetType {
        Stylesheet,
        Image,
        Form,
        Link
    }

    public class Asset {
        public HtmlNode Node { get; set; }
        public HtmlNode Parent { get => Node.ParentNode; }

        public string Text { get => Node.InnerText; }
        public string Id { get => Node.GetAttributeValue("id", "<not set>"); }
        public string Class { get => Node.GetAttributeValue("class", "<not set>"); }

        public virtual AssetType Type { get; }

        public Asset(HtmlNode htmlNode) {
            Node = htmlNode;
        }
    }

    public class FormAsset : Asset {
        public FormAsset(HtmlNode htmlNode) : base(htmlNode) { }

        public override AssetType Type { get => AssetType.Form; }

        public string Action { get => this.Node.GetAttributeValue("action", "<not set>"); }
        public List<HtmlNode> Fields {
            get => this.Node.Descendants("input").Where(node => {
                string type = node.GetAttributeValue("type", "");
                return type != "button" && type != "submit" && type != "";
            }).ToList();
        }

        public FormAsset Fill(string name, string value) {
            this.Node.Descendants("input").Where(node => {
                string _name = node.GetAttributeValue("name", "");
                return _name == name;
            }).ToList().ForEach(n => {
                n.SetAttributeValue("value", value);
            });
            return this;
        }

        public FormAsset FillId(string id, string value) {
            this.Node.Descendants("input").Where(node => {
                string _id = node.GetAttributeValue("id", "");
                return _id == id;
            }).ToList().ForEach(n => {
                n.SetAttributeValue("value", value);
            });
            return this;
        }
    }

    public class StylesheetAsset : Asset {
        public StylesheetAsset(HtmlNode htmlNode) : base(htmlNode) { }
        public override AssetType Type { get => AssetType.Stylesheet; }
    }

    public class ImageAsset : Asset {
        public ImageAsset(HtmlNode htmlNode) : base(htmlNode) { }
        public override AssetType Type { get => AssetType.Image; }
        public string Src { get => this.Node.GetAttributeValue("src", "<not set>"); }
    }

    public class LinkAsset : Asset {
        public LinkAsset(HtmlNode htmlNode) : base(htmlNode) { }
        public override AssetType Type { get => AssetType.Link; }
        public string Href { get => this.Node.GetAttributeValue("href", ""); }
    }
}
