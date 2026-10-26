from pathlib import Path
p=Path('Assets/SecretsThatBreathe/Scripts/Editor/DialoguePresentationValidation.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('var root = phone.transform.Find("FlowingDialogue");','var root = phone.khemDialogue.motionRoot;')
s=s.replace('            bool done = false;','''            root = phone.tonDialogue.motionRoot;
            body = phone.tonDialogue.sentence;
            speaker = phone.tonDialogue.speaker;
            bool done = false;''')
s=s.replace('float firstY = body.rectTransform.anchoredPosition.y;', 'float firstY = root.anchoredPosition.y;')
s=s.replace('body.rectTransform.anchoredPosition.y > firstY + 10f', 'root.anchoredPosition.y > firstY + 10f')
s=s.replace('var visitorRoot = visitor.transform.Find("FlowingDialogue");','var visitorRoot = visitor.visitorDialogue.motionRoot;')
p.write_text(s,encoding='utf-8')
