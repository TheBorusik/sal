using System;

namespace SAL.Infrastructure.ValidationAttribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NotEmptyArrayAttribute : Attribute
    {
    }
}
