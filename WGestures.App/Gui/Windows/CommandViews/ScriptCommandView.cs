﻿using WGestures.Core.Commands;
using WGestures.Core.Commands.Impl;

namespace WGestures.App.Gui.Windows.CommandViews
{
    public partial class ScriptCommandView : CommandViewUserControl
    {
        ScriptCommand _cmd;

        public ScriptCommandView()
        {
            InitializeComponent();
        }

        public override AbstractCommand Command
        {
            get { return _cmd; }
            set
            {
                _cmd = (ScriptCommand)value;
                txt_initScript.Text = _cmd.InitScript;
                txt_script.Text = _cmd.Script;
                check_handleModifiers.Checked = _cmd.HandleModifiers;
                txt_modifierRecognized.Text = _cmd.GestureRecognizedScript;
                txt_modifierTriggered.Text = _cmd.ModifierTriggeredScript;
                txt_gestureEnded.Text = _cmd.GestureEndedScript;
            }
        }

        private void txt_script_TextChanged(object sender, System.EventArgs e)
        {
            _cmd.Script = txt_script.Text;
        }

        private void txt_initScript_TextChanged(object sender, System.EventArgs e)
        {
            _cmd.InitScript = txt_initScript.Text;
        }

        private void check_handleModifiers_CheckedChanged(object sender, System.EventArgs e)
        {
            _cmd.HandleModifiers = check_handleModifiers.Checked;
        }

        private void txt_modifierRecognized_TextChanged(object sender, System.EventArgs e)
        {
            _cmd.GestureRecognizedScript = txt_modifierRecognized.Text;
        }

        private void txt_modifierTriggered_TextChanged(object sender, System.EventArgs e)
        {
            _cmd.ModifierTriggeredScript = txt_modifierTriggered.Text;
        }

        private void txt_gestureEnded_TextChanged(object sender, System.EventArgs e)
        {
            _cmd.GestureEndedScript = txt_gestureEnded.Text;
        }
    }
}
