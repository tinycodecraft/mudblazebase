﻿using System;

namespace blazelogBase.Shared.ExcelPart
{

    /// <summary>
    /// Exception used during loading process of defined names
    /// </summary>
    public class DefinedNameLoadException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DefinedNameLoadException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
