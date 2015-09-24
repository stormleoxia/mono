using System;
using System.ComponentModel;

namespace System.Data
{
	[AttributeUsage(AttributeTargets.All)]
	internal sealed class EDesignResDescriptionAttribute : DescriptionAttribute
	{
		private bool replaced;

		public override string Description
		{
			get
			{
				if (!this.replaced)
				{
					this.replaced = true;
					base.DescriptionValue = EDesignRes.GetString(base.Description);
				}
				return base.Description;
			}
		}

		public EDesignResDescriptionAttribute(string description) : base(description)
		{
		}
	}
}
