using System;

namespace Comander.Dtos
{
    public class CopyStatusDto
    {
        private bool _overrideAll = false;
        private bool _overrideAny = false;

        public bool OverrideAll
        {
            get
            {
                return _overrideAll;
            }
            set
            {
                if (OverrideAny && value)
                {
                    _overrideAny = false;
                    _overrideAll = value;
                }
            }
        }
        public bool OverrideAny
        {
            get
            {
                return _overrideAny;
            }
            set
            {
                if(OverrideAll && value)
                {
                    _overrideAll = false;
                    _overrideAny = value;
                }
            }
        }

        public void SetToDefault()
        {
            _overrideAny = false;
            _overrideAll = false;
        }
    }
}
