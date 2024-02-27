using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace UnityEditor.XR.VisionOS
{
    class Plist
    {
        class PlistTextWriter : XmlTextWriter
        {
            public PlistTextWriter(string filename, Encoding encoding)
                : base(filename, encoding) { }

            public override void WriteDocType(string name, string pubid, string sysid, string subset)
            {
                WriteRaw("\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
            }
        }
        
        public class Element
        {
            readonly XmlNode m_Node;

            public Element(XmlNode node) => m_Node = node ?? throw new ArgumentNullException(nameof(node));

            public Element this[string key]
            {
                get
                {
                    if (m_Node.Name != "dict")
                        throw new InvalidDataException($"Node '{m_Node.Name}' is not a dictionary.");

                    var value = EnumerateKeys(m_Node.ChildNodes)
                        .Where(k => k.InnerText == key)
                        .Select(k => new Element(k.NextSibling))
                        .FirstOrDefault();

                    if (value == null)
                        throw new KeyNotFoundException($"Key '{key}' not found.");

                    return value;
                }
                set
                {
                    var xmlDocument = m_Node.OwnerDocument;
                    if (xmlDocument == null)
                    {
                        Debug.LogError("Error processing Plist, node has no owner document.");
                        return;
                    }

                    var existingKey = EnumerateKeys(m_Node.ChildNodes).FirstOrDefault(k => k.InnerText == key);
                    if (existingKey != null)
                    {
                        var existingValue = existingKey.NextSibling;
                        m_Node.RemoveChild(existingKey);
                        if (existingValue != null)
                            m_Node.RemoveChild(existingValue);
                    }

                    var keyElement = xmlDocument.CreateElement("key");
                    keyElement.InnerText = key;
                    m_Node.AppendChild(keyElement);
                    m_Node.AppendChild(value.m_Node);
                }
            }

            public Element[] AsArray() => m_Node.Name == "array"
                ? EnumerateNodes(m_Node.ChildNodes).Select(node => new Element(node)).ToArray()
                : null;

            public string AsString() => m_Node.Name == "string"
                ? m_Node.InnerText
                : null;
        }

        XmlDocument m_XmlDocument;

        Plist(XmlDocument xmlDocument) =>
            m_XmlDocument = xmlDocument ?? throw new ArgumentNullException(nameof(xmlDocument));

        public static Plist ReadFromString(string contents)
        {
            var xml = new XmlDocument();
            xml.LoadXml(contents);
            return new Plist(xml);
        }

        public Element root
        {
            get
            {
                var child = m_XmlDocument.SelectSingleNode("child::plist/dict");
                return child != null ? new Element(child) : null;
            }
        }

        static IEnumerable<XmlNode> EnumerateNodes(XmlNodeList nodeList)
        {
            var enumerator = nodeList?.GetEnumerator();
            if (enumerator == null)
                yield break;

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current as XmlNode;
            }
        }

        static IEnumerable<XmlNode> EnumerateKeys(XmlNodeList nodeList) =>
            EnumerateNodes(nodeList).Where(node => node.Name == "key");

        public Element CreateElement(string name, string innerText = "")
        {
            var element = m_XmlDocument.CreateElement(name);
            if (!string.IsNullOrEmpty(innerText))
                element.InnerText = innerText;
            
            return new Element(element);
        }

        public void WriteToFile(string path)
        {
            var writer = new PlistTextWriter(path, Encoding.Default);
            writer.Formatting = Formatting.Indented;
            m_XmlDocument.WriteTo(writer);
            writer.Flush();
        }
    }
}
