# Lawyer Room dialogue Canvas editing

Dialogue presentation is saved in the scene. No dialogue GameObjects or TMP labels
are generated at runtime. Each speaker has a separate Canvas:

- SeatingCanvas / PhoneCallUI / KhemDialogueCanvas
- SeatingCanvas / PhoneCallUI / TonDialogueCanvas
- SeatingCanvas / AuntDialogueUI / KhemDialogueCanvas
- SeatingCanvas / AuntDialogueUI / UncleDialogueCanvas

Open FlowingDialogue under each Canvas:

- Speaker: edit the speaker name, colour, font and size directly on TMP.
- Sentence: edit the sentence colour, font, size and RectTransform directly.
- FlowingDialogue: move the entire dialogue block by changing its RectTransform.

White sentences all start at the same saved position: screen anchor (0.32, 0.16),
local X = 0, top Y = 33 for the original Kanit Medium font at size 30. This is the
same white-text starting position as the earlier layout with the name above it.
Names are now to the left, right-aligned with a two-space gap. Longer names extend
to the left and never push the white text to the right. TMP's Thai baseline is
normalized during animation so marks do not shift the first line between speakers.

Canvas objects are initially inactive to prevent preview text appearing before a
conversation. To preview in Edit mode, enable one speaker Canvas at a time.
Sentence content still comes from the existing Canvas sentence objects:
PhoneCallUI / Lines, PhoneCallUI / CallBackLines, and AuntDialogueUI / 01-08.
The Sentence TMP under a speaker Canvas controls the display's visual style.

PhoneCallDialogueUI / Presentation and AuntDialogueUI / Presentation now expose
only typing interval, completed-line hold and upward-scroll speed. Legacy colour
and layout fields are retained only for one-time scene migration and are hidden.
The general stack layout and global Thai font/grey-colour reset exclude the saved
dialogue views, so Inspector edits to those views remain authoritative.

Verification: runtime and Editor assemblies compiled with Unity's Roslyn compiler.
authored-canvas-validation.json checks the saved hierarchy, common white text
origin, font and size. Earlier play-validation.txt/screenshots document the previous
runtime-generated view; they are not evidence of a Play run of this Canvas refactor.

Money choices remain transparent full-area buttons. Configure their normal and
hover colours, scale and transition on AuntMoneyChoiceUI. The follow-up uses no
screen-control tools.

## Yellow quoted text
All six quoted source messages now match DocumentSearchUI/ReturnToDeskMessage: yellow (#FFFF00), Kanit Medium 25, parent anchor (0.5, 0.16), anchored position (0, -100), panel 1400x150. This includes PhoneCallBackThought, DrawerObjective, ReturnToDeskMessage and the three money-choice thoughts. The thoughts display through AuntDialogueUI/QuotedThoughtCanvas/FlowingDialogue/Sentence, whose saved TMP style matches the reminder. The general overlap layout keeps quoted reminders at their authored bottom position. quoted-style-validation.json verifies all six source objects plus the saved thought display. Runtime and Editor assemblies compiled successfully; no Play mode run was performed for this change.

## White money thoughts (latest behaviour)
Only the three thoughts after choosing Accept Money are now white. Their saved display is AuntDialogueUI/QuotedThoughtCanvas/FlowingDialogue/Sentence. The complete sentence appears immediately at the authored Canvas position, with no typewriter or movement. Money Thought Hold Duration on AuntDialogueUI defaults to 3 seconds; the existing story then automatically returns to the money choices. Early clicks cannot bypass this hold. Other objective quotes retain their yellow style. The three source TMPs and their display TMP are white in the saved scene; global Thai-text colour normalization excludes them. Both assemblies compile; the saved colours and 3-second setting are verified in white-money-thoughts-validation.json. No Play run was performed for this follow-up.

## Exit objective
SeatingCanvas/MealAndSearchUI/ExitObjective/PromptText is a saved yellow reminder cloned from ReturnToDeskMessage (Kanit Medium 25, same bottom position). It says ไปที่ประตูทางออก. DrawerSearchScareSequence waits Exit Objective Delay (default 3 seconds) after the uncle exit / stand-up fade completes and player control returns, then shows the reminder persistently. Cancel/reset stops the pending reminder and hides it. The style and message remain editable on Canvas. Both assemblies compile; saved scene reference, 3-second setting and matching yellow style were verified. No Play mode run was performed for this change.

## Dialogue typing audio and Uncle Pong
PhoneCallUI and AuntDialogueUI each contain a saved DialogueTypingAudio AudioSource using Assets/video-game-text-330163.mp3. It is a 2D looping source with Play On Awake off and initial Volume 0.35. Presentation / Typing Audio references the source. FlowingDialogue plays it only while typing, stops it before the completed-line hold, pauses it with game time, and stops it when hidden/cancelled. Instant money thoughts remain silent. Both incoming and callback phone dialogue use this path. The saved UncleDialogueCanvas/FlowingDialogue/Speaker now reads ลุงป้อง; colour, font and spacing are preserved. Adjust Volume on the corresponding DialogueTypingAudio object. Both assemblies compiled; typing-audio-validation.json verifies the saved clip links and display name. No Play/audio listening run was performed for this follow-up.

## Separate first-call request Canvas
The first phone call now has twelve entries. The location explanation ends before ฝากหาให้พี่ด้วยนะเข้ม; that request is the next entry and uses its own saved TonRequestDialogueCanvas. Khem's final โอเคครับ still follows it. Edit the request directly at SeatingCanvas/PhoneCallUI/TonRequestDialogueCanvas/FlowingDialogue/Sentence. Its text is also the sentence-list source, so no hidden duplicate must be edited. The new Canvas copies Ton's current name, colour, font and positioning and uses the same typing timing/audio source. Incoming call speaker order was updated; callback lines are unchanged. Both assemblies compile and ton-request-canvas-validation.json verifies the saved sequence and style. No Play mode run was performed for this change.

## Objective text colour #C0C000
All TMP texts previously using pure yellow in Lawyer Room now use #C0C000 (RGB 192,192,0; opacity unchanged). This covers the four objective messages and the empty legacy thought speaker label. White money thoughts remain white. Font, size, position and timing are unchanged. yellow-C0C000-validation.json verifies the saved TMP/Color32 values.

## Unused text cleanup reverted
The user requested undoing the cleanup. All 18 deleted GameObjects / 14 TMP texts and their original references were restored from the pre-cleanup snapshot. The cleanup-specific runtime/editor code changes were reverted, and UnusedTextCleanup.cs plus its meta were removed. Later unrelated scene changes were retained. undo-unused-cleanup-validation.json records the restoration. Historical cleanup reports/backups are retained only for audit.
