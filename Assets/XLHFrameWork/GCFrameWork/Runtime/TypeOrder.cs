using System;

namespace XLHFramework.GCFrameWork.Runtime
{
    public class TypeOrder
    {
        public readonly int order;

        public readonly Type type;
        
        public TypeOrder(int order, Type type)
        {
            this.order = order;
            this.type = type;
        }
    }
}