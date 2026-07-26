using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs tutorial lessons: plays a dialog and outlines a set of objects on first introduction
/// of a mechanic, then clears the outlines once the mechanic has been used successfully.
/// Each lesson is triggered/completed by string id, either from code or via inspector wiring.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private DialogController dialogController;
    [SerializeField] private TutorialLesson[] lessons;

    private Dictionary<string, TutorialLesson> lessonMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        lessonMap = new Dictionary<string, TutorialLesson>();
        if (lessons != null)
        {
            foreach (var lesson in lessons)
            {
                if (lesson == null || string.IsNullOrEmpty(lesson.id)) continue;
                lessonMap[lesson.id] = lesson;
                SetOutlines(lesson, false);
            }
        }
    }

    /// <summary>
    /// Fires the lesson if it hasn't been shown yet: plays the dialog and enables outlines.
    /// No-op if the lesson id doesn't exist or has already been shown.
    /// </summary>
    public void TriggerLesson(string id)
    {
        if (!lessonMap.TryGetValue(id, out var lesson)) return;
        if (lesson.shown) return;

        lesson.shown = true;
        if (lesson.dialog != null && dialogController != null)
            dialogController.StartDialogue(lesson.dialog, lesson.holdDialogUntilCompleted);
        SetOutlines(lesson, true);
        lesson.onTriggered?.Invoke();
    }

    /// <summary>
    /// Marks a shown lesson complete: hides its outlines. No-op if not shown or already completed.
    /// </summary>
    public void CompleteLesson(string id)
    {
        if (!lessonMap.TryGetValue(id, out var lesson)) return;
        if (!lesson.shown || lesson.completed) return;

        lesson.completed = true;
        SetOutlines(lesson, false);
        if (lesson.holdDialogUntilCompleted && dialogController != null)
            dialogController.ForceDismiss();
        lesson.onCompleted?.Invoke();
    }

    /// <summary>
    /// Plays a one-off dialog without any outlines or completion tracking. Useful for
    /// success/failure messages that don't need to gate on later interaction.
    /// </summary>
    public void PlayDialog(DialogObject dialog)
    {
        if (dialog != null && dialogController != null)
            dialogController.StartDialogue(dialog);
    }

    private static void SetOutlines(TutorialLesson lesson, bool value)
    {
        if (lesson.outlines == null) return;
        foreach (var outline in lesson.outlines)
        {
            if (outline != null) outline.enabled = value;
        }
    }
}

[Serializable]
public class TutorialLesson
{
    [Tooltip("Unique id used to Trigger and Complete this lesson.")]
    public string id;
    [Tooltip("Dialog that plays when this lesson is first triggered.")]
    public DialogObject dialog;
    [Tooltip("Outline components enabled during the lesson and disabled on completion.")]
    public Outline[] outlines;
    [Tooltip("If true, the dialog stays on its final line until CompleteLesson is called — the player cannot dismiss it early.")]
    public bool holdDialogUntilCompleted;
    [Tooltip("Fires when TriggerLesson is called for the first time.")]
    public UnityEvent onTriggered;
    [Tooltip("Fires when CompleteLesson clears the lesson.")]
    public UnityEvent onCompleted;

    [HideInInspector] public bool shown;
    [HideInInspector] public bool completed;
}
