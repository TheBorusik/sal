using System;

namespace SAL.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SalContourHandlerAttribute : Attribute
    {
        public Contour Contour { get; private set; }

        public SalContourHandlerAttribute(Contour contour)
        {
            Contour = contour;
        }
    }
    
    public enum Contour
    {
        Back,
        Front,
        Both,
    }
}