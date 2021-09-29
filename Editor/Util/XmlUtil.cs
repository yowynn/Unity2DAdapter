using System;
using System.Xml;
using System.IO;

namespace Wynncs.Util
{
    public class XmlUtil
    {
        /// <summary>
        /// 打开一个 XML 文档（或空文档）
        /// </summary>
        /// <param name="filePath">完整路径</param>
        /// <param name="nullIfNotExist">为 False，则在打开文档失败时新建一个空白文档</param>
        /// <returns>XML文档</returns>
        public static XmlDocument Open(string filePath = "", bool nullIfNotExist = false)
        {
           XmlDocument doc = new XmlDocument();
           try
            {
                doc.Load(filePath);
                return doc;
            }
            catch (Exception)
            {
                return nullIfNotExist ? null : doc;
            }
        }

        /// <summary>
        /// 获取子元素
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="elementName">子元素名</param>
        /// <returns>子元素</returns>
        public static XmlElement GetElement(XmlNode node, string elementName)
        {
            return node?[elementName];
        }

        /// <summary>
        /// 获取子元素
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="elementIndex">子元素索引</param>
        /// <returns>子元素</returns>
        public static XmlElement GetElement(XmlNode node, int elementIndex)
        {
            return node?.ChildNodes[elementIndex] as XmlElement;
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <returns>属性</returns>
        public static XmlAttribute GetAttribute(XmlNode node, string attrName)
        {
            return node?.Attributes?[attrName];
        }

        /// <summary>
        /// 获取属性值（string）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值（string）</returns>
        public static string GetAttributeString(XmlNode node, string attrName,string defaultValue = "")
        {
            var attr = GetAttribute(node, attrName);
            string value = attr == null ? defaultValue : attr.Value;
            return value;
        }

        /// <summary>
        /// 获取属性值（int）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值（int）</returns>
        public static int GetAttributeInt(XmlNode node, string attrName, int defaultValue = 0)
        {
            var attr = GetAttribute(node, attrName);
            if (attr == null || !Int32.TryParse(attr.Value, out int value)) value = defaultValue;
            return value;
        }

        /// <summary>
        /// 获取属性值（float）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值（float）</returns>
        public static float GetAttributeFloat(XmlNode node, string attrName, float defaultValue = 0f)
        {
            var attr = GetAttribute(node, attrName);
            if (attr == null || !Single.TryParse(attr.Value, out float value)) value = defaultValue;
            return value;
        }

        /// <summary>
        /// 获取属性值（double）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值（double）</returns>
        public static double GetAttributeDouble(XmlNode node, string attrName, double defaultValue = 0d)
        {
            var attr = GetAttribute(node, attrName);
            if (attr == null || !Double.TryParse(attr.Value, out double value)) value = defaultValue;
            return value;
        }

        /// <summary>
        /// 获取属性值（bool）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值（bool）</returns>
        public static bool GetAttributeBool(XmlNode node, string attrName, bool defaultValue = false)
        {
            var attr = GetAttribute(node, attrName);
            if (attr == null || !Boolean.TryParse(attr.Value, out bool value)) value = defaultValue;
            return value;
        }

        /// <summary>
        /// 添加子元素（在末尾）
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="elementName">子元素名</param>
        /// <returns>子元素</returns>
        public static XmlElement AddElement(XmlNode node, string elementName)
        {
            var doc = node as XmlDocument ?? node.OwnerDocument;
            XmlElement element = doc.CreateElement(elementName);
            node.AppendChild(element);
            return element;
        }

        /// <summary>
        /// 添加子元素到对应位置
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="elementName">子元素名</param>
        /// <param name="index">位置索引</param>
        /// <returns>子元素</returns>
        public static XmlElement AddElement(XmlNode node, string elementName, int index)
        {
            if (node.HasChildNodes && index < node.ChildNodes.Count)
            {
                var doc = node as XmlDocument ?? node.OwnerDocument;
                XmlElement element = doc.CreateElement(elementName);
                node.InsertBefore(element, node.ChildNodes[index]);
                return element;
            }
            else
            {
                return AddElement(node, elementName);
            }
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="attrValue">属性值</param>
        /// <returns>属性</returns>
        public static XmlAttribute SetAttribute(XmlNode node, string attrName, ValueType attrValue)
        {
            var doc = node as XmlDocument ?? node.OwnerDocument;
            XmlAttribute attr = doc.CreateAttribute(attrName);
            attr.Value = attrValue.ToString();
            node.Attributes.SetNamedItem(attr);
            return attr;
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="node">参照节点</param>
        /// <param name="attrName">属性名</param>
        /// <param name="attrValue">属性值</param>
        /// <returns>属性</returns>
        public static XmlAttribute SetAttribute(XmlNode node, string attrName, string attrValue = "")
        {
            var doc = node as XmlDocument ?? node.OwnerDocument;
            XmlAttribute attr = doc.CreateAttribute(attrName);
            attr.Value = attrValue;
            node.Attributes.SetNamedItem(attr);
            return attr;
        }

        /// <summary>
        /// 统计XML节点数量和引用结构
        /// </summary>
        /// <param name="sourse">要统计的节点</param>
        /// <param name="log">要记录到的 XML 文档</param>
        public static void Statistics(XmlNode sourse, XmlDocument log)
        {
            var root = GetElement(log, "Elements") ?? AddElement(log, "Elements");
            if (sourse is XmlElement)
            {
                var element = GetElement(root, sourse.Name) ?? AddElement(root, sourse.Name);
                if (sourse.Attributes != null && sourse.Attributes.Count > 0)
                {
                    foreach (XmlAttribute attr in sourse.Attributes)
                    {
                        SetAttribute(element, attr.Name, GetAttributeInt(element, attr.Name) + 1);
                    }
                }
                foreach (var _child in sourse)
                {
                    var child = _child as XmlElement;
                    if (child != null)
                    {
                        var node = GetElement(element, child.Name) ?? AddElement(element, child.Name);
                        SetAttribute(node, "Count", GetAttributeInt(node, "Count") + 1);
                        Statistics(child, log);
                    }
                }
            }
            else if (sourse is XmlDocument)
            {
                foreach (var _child in sourse)
                {
                    var child = _child as XmlElement;
                    if (child != null)
                    {
                        SetAttribute(root, child.Name, GetAttributeInt(root, child.Name) + 1);
                        Statistics(child, log);
                    }
                }
            }
        }

        /// <summary>
        /// 统计XML节点数量和引用结构
        /// </summary>
        /// <param name="sourseFilePath">要统计的 XML 文件完整路径</param>
        /// <param name="logFilePath">统计结果输出到的 XML 文件路径</param>
        /// <param name="isAppend">是否累加到原输出文件上</param>
        public static void Statistics(string sourseFilePath, string logFilePath, bool isAppend = false)
        {
            Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(logFilePath)));
            var sourse = Open(sourseFilePath);
            var log = isAppend ? Open(logFilePath) : Open();
            Statistics(sourse, log);
            log.Save(logFilePath);
        }
    }

}
