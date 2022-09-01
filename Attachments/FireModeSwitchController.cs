﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Attachments
{
    public class FireModeSwitchController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        /// Parent Class Controller ///
        private Weapons.BaseFirearmGenerator parentFirearm;
        /// Module Based References ///
        private Handle attachmentHandle;
        private AudioSource activationSound;
        private Transform pivotTransform;
        private List<Transform> switchPositions;
        private List<FireMode> switchModes;
        /// General Mechanics ///
        public float lastSpellMenuPress;
        public bool isLongPress = false;
        public bool checkForLongPress = false;
        public bool spellMenuPressed = false;

        public void NextFireMode()
        {
            if (parentFirearm != null)
            {
                // Get weapon current firemode, then iterate to next available index  
                int selectionIndex = switchModes.IndexOf(parentFirearm.GetCurrentFireMode());
                selectionIndex++;
                if ((selectionIndex == -1) ||(selectionIndex >= switchModes.Count)) selectionIndex = 0;
                parentFirearm.SetNextFireMode(switchModes[selectionIndex]);
                if (activationSound != null) activationSound.Play();
                //Finally, if we have a "physical switch", set that GameObject position
                try
                {
                    if (pivotTransform != null)
                    {
                        pivotTransform.position = switchPositions[selectionIndex].position;
                        pivotTransform.rotation = Quaternion.Euler(switchPositions[selectionIndex].rotation.eulerAngles);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(String.Format("[ModularFirearms][Exception] NextFireMode(): {0} \n {1}", e.Message, e.StackTrace));
                }
            }
            else
            {
                Debug.LogError("[ModularFirearms][ERROR] NextFireMode(): no parent firearm was found");
            }
        }
        void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
            switchPositions = new List<Transform>();
            switchModes = new List<FireMode>();
            // Get reference to attached weapon (current firemode selection)
            parentFirearm = this.GetComponent<Weapons.BaseFirearmGenerator>();
            if (!String.IsNullOrEmpty(module.attachmentHandleRef)) attachmentHandle = item.GetCustomReference(module.attachmentHandleRef).GetComponent<Handle>();
            if (!String.IsNullOrEmpty(module.activationSoundRef)) activationSound = item.GetCustomReference(module.activationSoundRef).GetComponent<AudioSource>();
            // Gameobject that which will move to match the reference positions
            if (!String.IsNullOrEmpty(module.swtichRef)) pivotTransform = item.GetCustomReference(module.swtichRef);
            // Swtich Positions are passed as custom references. pivotTransform is then matched to these transforms
            foreach (string switchPositionRef in module.switchPositionRefs)
            {
                Transform switchPosition = item.GetCustomReference(switchPositionRef);
                switchPositions.Add(switchPosition);
            }
            // allowed fire modes are matched to swtich positions based on list index
            foreach (string allowedFireMode in module.allowedFireModes)
            {
                FrameworkCore.FireMode switchMode = (FrameworkCore.FireMode)Enum.Parse(typeof(FrameworkCore.FireMode), allowedFireMode);
                switchModes.Add(switchMode);
            }
            // If we are using a moving swtich, check that our lists have mappable indicies
            if (pivotTransform != null)
            { 
                if (switchPositions.Count != switchModes.Count)
                {
                    Debug.LogWarning("WARNING, FireModeSwtich switchPositions and switchModes have different lengths!!!");
                }
            }
        }
        protected void StartLongPress()
        {
            checkForLongPress = true;
            lastSpellMenuPress = Time.time;
        }
        public void CancelLongPress()
        {
            checkForLongPress = false;
        }
        public void LateUpdate()
        {
            if (checkForLongPress)
            {
                if (spellMenuPressed)
                {

                    if ((Time.time - lastSpellMenuPress) > module.longPressTime)
                    {
                        // Long Press Detected
                        if (module.longPressToActivate) NextFireMode();
                        CancelLongPress();
                    }
                }
                else
                {
                    // Long Press Self Cancelled (released button before time)
                    // Short Press Detected
                    CancelLongPress();
                    if (!module.longPressToActivate) NextFireMode();
                }
            }
        }
        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(attachmentHandle))
            {
                // "Spell-Menu" Action
                if (action == Interactable.Action.AlternateUseStart)
                {
                    spellMenuPressed = true;
                    StartLongPress();
                }
                else if (action == Interactable.Action.AlternateUseStop) spellMenuPressed = false;
            }
        }
    }
}