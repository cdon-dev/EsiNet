﻿using System.Collections.Generic;

namespace EsiNet
{
    public class EsiIncludeParser : IEsiParser
    {
        public IEsiFragment Parse(IReadOnlyDictionary<string, string> attributes, string body)
        {
            var src = attributes["src"];

            return new EsiIncludeFragment(src);
        }
    }
}