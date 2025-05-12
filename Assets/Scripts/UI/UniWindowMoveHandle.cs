/*
 * UniWindowDragMove.cs
 * 
 * Author: Kirurobo http://twitter.com/kirurobo
 * License: MIT
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_LEGACY_INPUT_MANAGER
#elif ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace App
{
    public class UniWindowMoveHandle : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerUpHandler
    {
        private Kirurobo.UniWindowController _uniwinc;
        /// <summary>
        /// �E�B���h�E���ő剻����Ă���Ƃ��͈ړ��𖳌��ɂ��邩
        /// </summary>
        [Tooltip("Disable drag-move when the window is zoomed (maximized).")]
        public bool disableOnZoomed = true;

        /// <summary>
        /// �h���b�O���Ȃ� true
        /// </summary>
        public bool IsDragging
        {
            get { return _isDragging; }
        }
        private bool _isDragging = false;

        /// <summary>
        /// �h���b�O���s�Ȃ����ۂ�
        /// </summary>
        private bool IsEnabled
        {
            get { return enabled && (!disableOnZoomed || !IsZoomed); }
        }

        /// <summary>
        /// ���j�^�Ƀt�B�b�g�����邩�A�ő剻���Ă���
        /// </summary>
        private bool IsZoomed
        {
            get { return (_uniwinc && (_uniwinc.shouldFitMonitor || _uniwinc.isZoomed)); }
        }

        /// <summary>
        /// �h���b�O�O�ɂ͎����q�b�g�e�X�g���L�������������L��
        /// </summary>
        private bool _isHitTestEnabled;

        /// <summary>
        /// �h���b�O�J�n���̃E�B���h�E�����W[px]
        /// </summary>
        private Vector2 _dragStartedPosition;

        // Start is called before the first frame update
        void Start()
        {
            // �V�[������ UniWindowController ���擾
            _uniwinc = GameObject.FindAnyObjectByType<Kirurobo.UniWindowController>();
            if (_uniwinc) _isHitTestEnabled = _uniwinc.isHitTestEnabled;

            //// ���Ȃ��Ă��ǂ������Ȃ̂ŏ���ɕύX���Ȃ��悤�R�����g�A�E�g
            //Input.simulateMouseWithTouches = false;
        }

        /// <summary>
        /// �h���b�O�J�n���̏���
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log("OnBeginDrag");
            Debug.Log("IsVRMClicked: " + VRMClickTracker.IsVRMClicked+"\nIsEnabled:"+IsEnabled);
            if (!IsEnabled)
            {
                return;
            }

            // Mac���Ƌ�����ς���
            //  ���ۂ�Retina�T�|�[�g���L���̂Ƃ����������A
            //  eventData.position �̌n�ƁA�E�B���h�E���W�n�ŃX�P�[������v���Ȃ��Ȃ��Ă��܂�
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            _dragStartedPosition = _uniwinc.windowPosition - _uniwinc.cursorPosition;
#else
            _dragStartedPosition = eventData.position;
#endif

            // _isDragging �� false �Ȃ炱�ꂩ��h���b�O�J�n�Ɣ��f
            if (!_isDragging)
            {
                // �h���b�O���̓q�b�g�e�X�g�𖳌��ɂ���
                _isHitTestEnabled = _uniwinc.isHitTestEnabled;
                _uniwinc.isHitTestEnabled = false;
                _uniwinc.isClickThrough = false;
            }

            _isDragging = true;
        }

        /// <summary>
        /// �h���b�O�I�����̏���
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            EndDragging();
        }

        /// <summary>
        /// �}�E�X���オ�����ۂ��h���b�O�I���Ƃ݂Ȃ�
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerUp(PointerEventData eventData)
        {
            EndDragging();
        }

        /// <summary>
        /// �h���b�O�I���Ƃ���
        /// </summary>
        private void EndDragging()
        {
            if (_isDragging)
            {
                _uniwinc.isHitTestEnabled = _isHitTestEnabled;
            }
            _isDragging = false;
        }

        /// <summary>
        /// �ő剻���ȊO�Ȃ�A�}�E�X�h���b�O�ɂ���ăE�B���h�E���ړ�
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_uniwinc || !_isDragging) return;

            // �h���b�O�ł̈ړ�������������Ă����ꍇ
            if (!IsEnabled)
            {
                EndDragging();
                return;
            }

            // Move the window when the left mouse button is pressed
            if (eventData.button != PointerEventData.InputButton.Left) return;

            // Return if any modifier key is pressed
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return;
#elif ENABLE_INPUT_SYSTEM
            if (Keyboard.current[Key.LeftShift].isPressed || Keyboard.current[Key.RightShift].isPressed
                || Keyboard.current[Key.LeftCtrl].isPressed || Keyboard.current[Key.RightCtrl].isPressed
                || Keyboard.current[Key.LeftAlt].isPressed || Keyboard.current[Key.RightAlt].isPressed) return;
#endif

            // �t���X�N���[���Ȃ�E�B���h�E�ړ��͍s��Ȃ�
            //  �G�f�B�^���� true �ɂȂ��Ă��܂��悤�Ȃ̂ŁA�G�f�B�^�ȊO�ł̂݊m�F
#if !UNITY_EDITOR
            if (Screen.fullScreen)
            {
                EndDragging();
                return;
            }
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Mac�̏ꍇ�A�l�C�e�B�u�v���O�C���ł̃J�[�\���ʒu�擾�E�ݒ�
            _uniwinc.windowPosition = _uniwinc.cursorPosition + _dragStartedPosition;
#else
            // Windows�Ȃ�A�^�b�`������Ή������邽�߂� eventData.position ���g�p����
            // �X�N���[���|�W�V�������J�n���̈ʒu�ƈ�v�����镪�����E�B���h�E���ړ�
            _uniwinc.windowPosition += eventData.position - _dragStartedPosition;
#endif
        }
    }
}
