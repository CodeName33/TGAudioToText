import http.server
import socketserver
import json
from sbert_punc_case_ru import SbertPuncCase

class JSONRequestHandler(http.server.SimpleHTTPRequestHandler):
    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        data = self.rfile.read(content_length).decode('utf-8')
        
        try:
            json_data = json.loads(data)
        except json.JSONDecodeError:
            self.send_error(400, "Invalid JSON")
            return
        
        print("Received:", json_data['text'])

        print("Punctuating...")
        text = model.punctuate(json_data['text'])
        print("Done: ", text)

        response = {
            'text': text,
        }
        
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        response_bytes = json.dumps(response).encode('utf-8')
        self.wfile.write(response_bytes)

PORT = 8018

handler_object = JSONRequestHandler

with socketserver.TCPServer(("", PORT), handler_object) as httpd:
    print("Loading model...")
    model = SbertPuncCase()
    print("Model loaded")
    print("Serving at port", PORT)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        # Graceful shutdown on CTRL+C
        print("Server stopped by user request")
        httpd.shutdown()
