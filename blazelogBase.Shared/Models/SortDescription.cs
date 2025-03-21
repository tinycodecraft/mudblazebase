using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Models
{
    public struct SortDescription
    {
        private string _propertyName;

        private ListSortDirection _direction;

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
            set
            {

                _propertyName = value;
            }
        }

        public ListSortDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {

                if (value < ListSortDirection.Ascending || value > ListSortDirection.Descending)
                {
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(ListSortDirection));
                }

                _direction = value;
            }
        }

        public SortDescription(string propertyName, ListSortDirection direction)
        {
            if (direction != 0 && direction != ListSortDirection.Descending)
            {
                throw new InvalidEnumArgumentException("direction", (int)direction, typeof(ListSortDirection));
            }

            _propertyName = propertyName;
            _direction = direction;
            
        }
        public override bool Equals(object obj)
        {
            if (!(obj is SortDescription))
            {
                return false;
            }

            return this == (SortDescription)obj;
        }

        public static bool operator ==(SortDescription sd1, SortDescription sd2)
        {
            if (sd1.PropertyName == sd2.PropertyName)
            {
                return sd1.Direction == sd2.Direction;
            }

            return false;
        }

        public static bool operator !=(SortDescription sd1, SortDescription sd2)
        {
            return !(sd1 == sd2);
        }

        public override int GetHashCode()
        {
            int num = Direction.GetHashCode();
            if (PropertyName != null)
            {
                num = PropertyName.GetHashCode() + num;
            }

            return num;
        }


    }
}
