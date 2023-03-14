using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZCU.TechnologyLab.DepthClient.ViewModels;

namespace ZCU.TechnologyLab.DepthClient.DataModel
{
    public class FilterData : NotifyingClass
    {
        MainViewModel mvm;

        // filters
        private readonly bool[] _filters = Enumerable.Repeat(true, 6).ToArray();
        private bool _pointFilter = true;

        // filter values
        private double _thresholdSlider = 0.2;
        private int _linScaleFac = 2;
        private float _minValueTh = 0.15f;
        private float _maxValueTh = 2f;
        private int _iterationsSpat = 2;
        private float _alphaSpat = 0.5f; // 1-0.5
        private int _deltaSpat = 20;
        private int _holeSpat = 0;
        private float _alphaTemp = 0.6f; // 1-0.4
        private int _deltaTemp = 20;
        private int _persIndex = 3;
        private int _holeMethod = 1;

        public double ThresholdSlider
        {
            get => _thresholdSlider;
            set
            {
                _thresholdSlider = value;
                mvm.UpdateSliderModel();
            }
        }

        public bool[] Filters
        {
            get => _filters;
        }

        public bool Filter0
        {
            get => _filters[0];
            set
            {
                this._filters[0] = value;
                mvm.OnFilterChange();
            }
        }

        public bool Filter1
        {
            get => _filters[1];
            set
            {
                this._filters[1] = value;
                mvm.OnFilterChange();
            }
        }

        public bool Filter2
        {
            get => _filters[2];
            set
            {
                this._filters[2] = value;
                mvm.OnFilterChange();
            }
        }

        public bool Filter3
        {
            get => _filters[3];
            set
            {
                this._filters[3] = value;
                mvm.OnFilterChange();
            }
        }

        public bool Filter4
        {
            get => _filters[4];
            set
            {
                this._filters[4] = value;
                mvm.OnFilterChange();
            }
        }

        public bool Filter5
        {
            get => _filters[5];
            set
            {
                this._filters[5] = value;
                mvm.OnFilterChange();
            }
        }

        public bool PointFilter
        {
            get => _pointFilter;
            set
            {
                this._pointFilter = value;
                mvm.OnPointVisibilityChange();
            }
        }

        public float MinValueТh
        {
            get => _minValueTh;
            set
            {
                _minValueTh = value;
                mvm.OnFilterChange();
            }
        }

        public float MaxValueTh
        {
            get => _maxValueTh;
            set
            {
                _maxValueTh = value;
                mvm.OnFilterChange();
            }
        }

        public float AlphaSpat
        {
            get => _alphaSpat;
            set
            {
                _alphaSpat = value;
                mvm.OnFilterChange();
            }
        }

        public int DeltaSpat
        {
            get => _deltaSpat;
            set
            {
                _deltaSpat = value;
                mvm.OnFilterChange();
            }
        }

        public float AlphaTemp
        {
            get => _alphaTemp;
            set
            {
                _alphaTemp = value;
                mvm.OnFilterChange();
            }
        }

        public int DeltaTemp
        {
            get => _deltaTemp;
            set
            {
                _deltaTemp = value;
                mvm.OnFilterChange();
            }
        }

        public int LinScaleFac
        {
            get => _linScaleFac;
            set
            {
                _linScaleFac = value;
                mvm.OnFilterChange();
            }
        }

        public int IterationsSpat
        {
            get => _iterationsSpat;
            set
            {
                _iterationsSpat = value;
                mvm.OnFilterChange();
            }
        }

        public int HoleSpat
        {
            get => _holeSpat;
            set
            {
                _holeSpat = value;
                mvm.OnFilterChange();
            }
        }

        public int PersIndex
        {
            get => _persIndex;
            set
            {
                _persIndex = value;
                mvm.OnFilterChange();
            }
        }

        public int HoleMethod
        {
            get => _holeMethod;
            set
            {
                _holeMethod = value;
                mvm.OnFilterChange();
            }
        }

        public FilterData()
        {
        }

        public FilterData(MainViewModel mvm)
        {
            this.mvm = mvm;
        }

    }
}
