using System.Reflection;

namespace ModelCreator.Common
{
    public static class CloneUtils
    {
        // Code from
        // https://www.codeproject.com/Articles/42221/Constructing-an-instance-class-from-its-base-class
        /// <summary>
        /// construct a derived class of from a base class
        /// </summary>
        /// <typeparam name="F">type of base class</typeparam>
        /// <typeparam name="T">type of class you want</typeparam>
        /// <param name="Base">the instance of the base class</param>
        /// <returns></returns>
        public static T ConstructAndFill<F, T>(F Base) where T : F, new()
        {
            
            // create derived instance
            T derived = new T();
            // get all base class properties
            PropertyInfo[] properties = typeof(F).GetProperties();
            foreach (PropertyInfo bp in properties)
            {
                // get derived matching property
                PropertyInfo dp = typeof(T).GetProperty(bp.Name, bp.PropertyType);

                // this property must not be index property
                if (
                    (dp != null)
                    && (dp.GetSetMethod() != null)
                    && (bp.GetIndexParameters().Length == 0)
                    && (dp.GetIndexParameters().Length == 0)
                )
                    dp.SetValue(derived, dp.GetValue(Base, null), null);
            }

            return derived;
        }
    }
}
