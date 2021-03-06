﻿namespace JudoPayDotNet.Errors
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    // ReSharper disable UnusedMember.Global
	/// <summary>
	/// This model represents one issue encountered with a supplied field
	/// </summary>
    public class JudoModelError
    {
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
        public string DetailErrorMessage { get; set; }
    }
    // ReSharper restore UnusedMember.Global
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}
