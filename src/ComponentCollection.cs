using System.Collections.Generic;

namespace HL7.Dotnetcore
{
    internal class ComponentCollection : List<Component>
    {
        
        
        public ComponentCollection()
        {}

        public ComponentCollection(int initialCapacity) : base(initialCapacity)
        {}


        
        /// <summary>
        /// Component indexer
        /// </summary>
        internal new Component this[int index]
        {
            get
            {
                Component component = null;

                if (index < base.Count)
                    component = base[index];

                return component;
            }
            set
            {
                base[index] = value;
            }
        }

        /// <summary>
        /// Add Component at next position
        /// </summary>
        /// <param name="component">Component</param>
        internal new void Add(Component component)
        {
            base.Add(component);
        }

        /// <summary>
        /// Add component at specific position
        /// </summary>
        /// <param name="component">Component</param>
        /// <param name="position">Position</param>
        internal void Add(Component component, int position)
        {
            int listCount = base.Count;
            position = position - 1;

            if (position < listCount)
            {
                base[position] = component;
            }
            else
            {
                for (int comIndex = listCount; comIndex < position; comIndex++)
                {
                    Component blankComponent = new Component(component.Encoding);
                    blankComponent.Value = string.Empty;
                    base.Add(blankComponent);
                }

                base.Add(component);
            }
        }
    }
}
