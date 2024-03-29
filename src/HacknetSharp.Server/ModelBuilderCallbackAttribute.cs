﻿using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Marks a method on a database entity type as a model builder callback for initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModelBuilderCallbackAttribute : Attribute
    {
    }
}
