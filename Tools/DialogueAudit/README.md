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
