from sbert_punc_case_ru import SbertPuncCase
model = SbertPuncCase()
source = "this is english text we want to see how good it will now we can relax ok"
text = model.punctuate(source)
print("Source text: ", source)
print("Punctuated text: ", text)