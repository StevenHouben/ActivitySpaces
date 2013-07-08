using System;
using System.ComponentModel;
using System.Windows.Forms;
using ABC.PInvoke;

namespace ActivitySpaces.Input
{
    public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    public class KeyboardHook
    {
        #region Private Members
        const int WmHotkey = 0x312;
        #endregion

        #region Private Members
        private NativeWindowEx hotKeyWin;
        #endregion

        #region Events
        public event EventHandler<KeyPressedEventArgs> KeyPressed;
        #endregion

        #region Constructor
        public KeyboardHook()
        {
            hotKeyWin = new NativeWindowEx();
            hotKeyWin.CreateHandle(new CreateParams());
            hotKeyWin.MessageRecieved += HotKeyWinMessageRecieved;
        }
        #endregion

        #region Private Methods
        private void HotKeyWinMessageRecieved(ref Message m)
        {
            if ( m.Msg != WmHotkey ) return;
            var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
            var modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

            if (KeyPressed != null)
                KeyPressed(this, new KeyPressedEventArgs(modifier, key));
        }
        #endregion

        #region Public Methods
        public void RegisterHotKey(int id, ModifierKeys modifiers, Keys keyCode)
        {
            if (id < 0 | id > 0xBFFF)
                throw new ArgumentException("Key code out of range. Range from O to 0xBFFF");
            if (modifiers == 0)
                throw new ArgumentException("You need at least one modifier key");
            if (User32.RegisterHotKey(hotKeyWin.Handle, id, (uint)modifiers, (uint)keyCode) == false)
            {
                throw new Win32Exception();
            }
        }
        public bool TryRegisterHotKey(int id, ModifierKeys modifiers, Keys keyCode)
        {
            return User32.RegisterHotKey(hotKeyWin.Handle, id, (uint)modifiers, (uint)keyCode);
        }
        public void UnregisterHotKey(int id)
        {
            if (User32.UnregisterHotKey(hotKeyWin.Handle, id) == false)
                throw new Win32Exception();
        }
        public bool TryUnregisterHotKey(int id)
        {
            return User32.UnregisterHotKey(hotKeyWin.Handle, id);
        }
        #endregion
    }
    public enum ModifierKeys
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
    public class KeyPressedEventArgs : EventArgs
    {
        private ModifierKeys modifier;
        private Keys key;
        internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            this.modifier = modifier;
            this.key = key;
        }
        public ModifierKeys Modifier
        {
            get { return modifier; }
        }
        public Keys Key
        {
            get { return key; }
        }
    }
}
