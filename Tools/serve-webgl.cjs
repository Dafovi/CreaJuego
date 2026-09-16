const http = require("http");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "Builds", "WebGLSpike");
const mime = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".wasm": "application/wasm",
  ".data": "application/octet-stream",
  ".unityweb": "application/octet-stream",
  ".css": "text/css; charset=utf-8",
  ".png": "image/png",
  ".ico": "image/x-icon"
};

http.createServer((request, response) => {
  let relative = decodeURIComponent(request.url.split("?")[0]);
  if (relative === "/") relative = "/index.html";
  const file = path.resolve(root, "." + relative);
  if (!file.startsWith(root + path.sep) && file !== path.join(root, "index.html")) {
    response.writeHead(403);
    return response.end();
  }
  fs.readFile(file, (error, data) => {
    if (error) {
      response.writeHead(404);
      return response.end("No encontrado");
    }
    response.writeHead(200, {
      "Content-Type": mime[path.extname(file)] || "application/octet-stream",
      "Content-Length": data.length,
      "Cache-Control": "no-store"
    });
    response.end(data);
  });
}).listen(8000, "127.0.0.1", () => {
  console.log("CreaJuego WebGL: http://127.0.0.1:8000/");
});