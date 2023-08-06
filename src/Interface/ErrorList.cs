using RWCustom;
using SlugBase.Assets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SlugBase.Interface
{
    internal class ErrorList
    {
        public static ErrorList Instance;

        private readonly List<ErrorInfo> _errors = new();
        private readonly FContainer _container;
        private readonly FSprite _icon;
        private readonly FContainer _list;
        private FSprite _listBack;
        private bool _open;
        private bool _listDirty;

        private const float X_MARGIN = 10f;
        private const float Y_MARGIN = 10f;
        private const float TEXT_WIDTH = 400f;
        private const float ENTRY_HEIGHT = 47f;
        private const float ICON_SIZE = 16f;
        private const float ELEMENT_SPACING = 7f;
        private const float Y_TEXT_OFFSET = 2f;

        public FContainer Container => _container;

        public ErrorList()
        {
            _container = new FContainer();
            _icon = new FSprite("slugbase/ui/error")
            {
                anchorX = 0f,
                anchorY = 1f
            };
            _list = new FContainer();

            _container.AddChild(_icon);
            _container.AddChild(_list);
        }

        public static ErrorList Attach()
        {
            var list = new ErrorList();
            list.Container.ListenForUpdate(list.Update);
            Futile.stage.AddChild(list.Container);
            
            SlugBaseCharacter.Registry.LoadFailed += (_, args) =>
            {
                Action retry = null;
                if (args.Path != null)
                    retry = () => SlugBaseCharacter.Registry.TryAddFromFile(args.Path);

                list.AddError(ErrorIcon.Character, args.ErrorMessage, args.Path, retry);

                Debug.LogException(args.Exception);
            };

            CustomScene.Registry.LoadFailed += (_, args) =>
            {
                Action retry = null;
                if (args.Path != null)
                    retry = () => CustomScene.Registry.TryAddFromFile(args.Path);

                list.AddError(ErrorIcon.Scene, args.ErrorMessage, args.Path, retry);

                Debug.LogException(args.Exception);
            };

            CustomSlideshow.Registry.LoadFailed += (_, args) =>
            {
                Action retry = null;
                if (args.Path != null)
                    retry = () => CustomSlideshow.Registry.TryAddFromFile(args.Path);

                list.AddError(ErrorIcon.Scene, args.ErrorMessage, args.Path, retry);

                Debug.LogException(args.Exception);
            };

            return list;
        }

        public void ClearFileErrors()
        {
            lock(_errors)
            {
                _errors.RemoveAll(error => error.File != null);
            }
        }

        public void MarkDirty()
        {
            _listDirty = true;
        }

        public void AddError(ErrorIcon icon, string message, string file, Action retry)
        {
            lock(_errors)
            {
                _listDirty = true;
                if (file != null)
                    _errors.RemoveAll(error => error.File == file);
                _errors.Add(new ErrorInfo(icon, message, file, retry));
            }
        }

        private void RemoveError(ErrorInfo error)
        {
            lock(_errors)
            {
                _errors.Remove(error);
                _listDirty = true;
            }
        }

        public void Update()
        {
            Container.SetPosition(X_MARGIN + 0.1f, Futile.screen.pixelHeight - Y_MARGIN + 0.1f);
            Container.MoveToFront();

            lock (_errors)
            {
                if (_listDirty)
                {
                    RefreshList();
                    _listDirty = false;
                }

                if (_icon.localRect.Contains(_icon.GetLocalMousePosition()))
                {
                    _open = true;
                }
                else if (_listBack != null && !_listBack.localRect.Contains(_listBack.GetLocalMousePosition()))
                {
                    _open = false;
                }

                if (_errors.Count == 0)
                    _open = false;

                _container.isVisible = _errors.Count > 0;
                _icon.isVisible = !_open;
                _list.isVisible = _open;
            }
        }

        private void RefreshList()
        {
            _list.RemoveAllChildren();

            float maxTextWidth = TEXT_WIDTH;

            var back = new FSprite("pixel")
            {
                anchorX = 0f,
                anchorY = 1f,
                height = ELEMENT_SPACING + (ENTRY_HEIGHT + ELEMENT_SPACING) * _errors.Count
            };
            _listBack = back;
            _list.AddChild(back);

            float y = -ELEMENT_SPACING;
            foreach(var error in _errors)
            {
                float x = ELEMENT_SPACING;

                var icon = new FSprite(GetIconSprite(error.Icon))
                {
                    anchorX = 0f,
                    anchorY = 1f
                };
                icon.SetPosition(x, y);

                _list.AddChild(icon);
                x += ICON_SIZE + ELEMENT_SPACING;

                // Add a button with a click handler
                void AddButton(string sprite, FButton.ButtonSignalDelegate onClick)
                {
                    var button = new FButton(sprite, sprite, sprite, null)
                    {
                        anchorX = 0f,
                        anchorY = 1f,
                    };
                    button.SetColors(SlugBasePlugin.SlugBaseBlue, Color.black, SlugBasePlugin.SlugBaseGray);
                    button.SetPosition(x, y);

                    _list.AddChild(button);

                    button.SignalRelease += onClick;
                }

                // Remove error and call a retry delegate
                if (error.Retry != null)
                {
                    AddButton("slugbase/ui/reload", _ =>
                    {
                        RemoveError(error);
                        error.Retry();
                    });
                }
                x += ICON_SIZE + ELEMENT_SPACING;

                // Open broken file in text editor
                if (error.File != null)
                {
                    AddButton("slugbase/ui/edit", _ => Application.OpenURL("file:///" + error.File));
                }
                x += ICON_SIZE + ELEMENT_SPACING;

                // Ignore error
                AddButton("slugbase/ui/cancel", _ => RemoveError(error));
                x += ICON_SIZE + ELEMENT_SPACING;

                // Add text
                var text = new StringBuilder();
                text.AppendLine(error.Message);
                if(error.File != null)
                {
                    var file = error.File;
                    if (error.File.Replace('\\', '/').StartsWith(Custom.RootFolderDirectory().Replace('\\', '/'), StringComparison.InvariantCultureIgnoreCase))
                        file = file.Substring(Custom.RootFolderDirectory().Length);

                    text.AppendLine($"File: {file}");
                }

                var label = new FLabel(Custom.GetFont(), text.ToString())
                {
                    anchorX = 0f,
                    anchorY = 1f,
                    color = Color.black
                };
                label.SetPosition(x, y + Y_TEXT_OFFSET);
                _list.AddChild(label);

                maxTextWidth = Math.Max(maxTextWidth, label.textRect.width);

                y -= ENTRY_HEIGHT + ELEMENT_SPACING;
            }

            back.width = ELEMENT_SPACING * 6 + ICON_SIZE * 4 + maxTextWidth;
        }

        private string GetIconSprite(ErrorIcon icon) => icon switch
        {
            ErrorIcon.Character => "slugbase/ui/character",
            ErrorIcon.Scene => "slugbase/ui/scene",
            ErrorIcon.Plugin => "slugbase/ui/plugin",
            _ => "pixel"
        };

        public void Remove()
        {
            _container.RemoveFromContainer();
        }

        private class ErrorInfo
        {
            public readonly ErrorIcon Icon;
            public readonly string Message;
            public readonly string File;
            public readonly Action Retry;

            public ErrorInfo(ErrorIcon icon, string message, string file, Action retry)
            {
                Icon = icon;
                Message = message;
                File = file;
                Retry = retry;
            }
        }

        public enum ErrorIcon
        {
            Character,
            Scene,
            Plugin
        }
    }
}
