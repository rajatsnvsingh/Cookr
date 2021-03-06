﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Cookr
{
    [Serializable]
    public class ToolTip
    {
        [XmlAttribute("Id")]
        public int Id;

        [XmlAttribute("Text")]
        public string Text;

        [XmlArray("Images"), XmlArrayItem("image")]
        public List<string> Images;
    }
}
