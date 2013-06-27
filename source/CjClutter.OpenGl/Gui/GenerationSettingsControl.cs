using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using CjClutter.OpenGl.Noise;
using Gwen;
using Gwen.Control;

namespace CjClutter.OpenGl.Gui
{
    public class GenerationSettingsControl
    {
        private int _octaves;
        private double _amplitude;
        private double _frequency;
        private readonly Properties _properties;
        private readonly DockWithBackground _dockWithBackground;
        private readonly Button _button;

        private readonly IList<Base> _invalidControls = new List<Base>();
        private List<Base> _propertyRows = new List<Base>();

        public GenerationSettingsControl(Base parent)
        {
            _dockWithBackground = new DockWithBackground(parent)
                {
                    Dock = Pos.Top,
                    Padding = new Padding(5, 5, 5, 5),
                    BackgroundColor = Color.LightBlue,
                };

            _properties = new Properties(_dockWithBackground)
                {
                    Dock = Pos.Top,
                };
            
            _button = new Button(_dockWithBackground)
                {
                    Dock = Pos.Top,
                    Margin = new Margin(0, 5, 0, 0),
                    Text = "Apply",
                    AutoSizeToContents = true,
                };

            _button.Clicked += OnSettingsChange;

            Refresh();
        }

        

        private void Refresh()
        {
            foreach (var propertyRow in _propertyRows)
            {
                _properties.RemoveChild(propertyRow, true);
            }

            AddField("Octaves", () => _octaves, x => _octaves = x);
            AddField("Amplitude", () => _amplitude, x => _amplitude = x);
            AddField("Frequency", () => _frequency, x => _frequency = x);
        }


        public event Action<FractalBrownianMotionSettings> GenerationSettingsChanged;

        private void OnSettingsChange(Base control)
        {
            if (GenerationSettingsChanged != null)
                GenerationSettingsChanged(GetSettings());
        }

        private void AddField<T>(string label, Func<T> getter, Action<T> setter)
        {
            var propertyRow = _properties.Add(label);
            propertyRow.SizeToChildren();

            propertyRow.Value = getter().ToString();
            propertyRow.ValueChanged += x =>
                {
                    var text = propertyRow.Value;
                    var converter = TypeDescriptor.GetConverter(typeof (T));

                    try
                    {
                        var newValue = (T) converter.ConvertFromInvariantString(text);
                        setter(newValue);

                        RemoveValidControl(propertyRow);
                    }
                    catch (Exception e)
                    {
                        AddInvalidControl(propertyRow);
                    }
                };

            var propertyRowContainer = new PropertyRowContainer<T>(propertyRow);

        }

        private void AddInvalidControl(Base control)
        {
            if (!_invalidControls.Contains(control))
                _invalidControls.Add(control);

            _button.IsDisabled = _invalidControls.Any();
        }

        private void RemoveValidControl(Base control)
        {
            _invalidControls.Remove(control);
            _button.IsDisabled = _invalidControls.Any();
        }

        private FractalBrownianMotionSettings GetSettings()
        {
            return new FractalBrownianMotionSettings(_octaves, _amplitude, _frequency);
        }

        public void SetSettings(FractalBrownianMotionSettings settings)
        {
            _octaves = settings.Octaves;
            _amplitude = settings.Amplitude;
            _frequency = settings.Frequency;

            Refresh();
        }

        public void Update()
        {
            //todo: update here or in render or instantiation only?
            //bool sizeToChildren = _properties.SizeToChildren();
        }
    }
}