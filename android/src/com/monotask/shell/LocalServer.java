package com.monotask.shell;

import android.content.res.AssetManager;

import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketTimeoutException;
import java.net.URLDecoder;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ThreadFactory;

public final class LocalServer {
    private static LocalServer instance;

    private final AssetManager assets;
    private final ServerSocket serverSocket;
    private final ExecutorService workers;

    public static synchronized LocalServer start(android.content.Context context, int port) {
        if (instance != null) return instance;
        try {
            instance = new LocalServer(context.getAssets(), port);
        } catch (IOException error) {
            instance = null;
        }
        return instance;
    }

    private LocalServer(AssetManager assets, int port) throws IOException {
        this.assets = assets;
        this.serverSocket = new ServerSocket();
        this.serverSocket.setReuseAddress(true);
        this.serverSocket.bind(new InetSocketAddress(
                InetAddress.getByAddress(new byte[] { 127, 0, 0, 1 }), port));
        this.workers = Executors.newCachedThreadPool(new ThreadFactory() {
            @Override
            public Thread newThread(Runnable runnable) {
                Thread thread = new Thread(runnable, "monotask-assets");
                thread.setDaemon(true);
                return thread;
            }
        });

        Thread acceptThread = new Thread(new Runnable() {
            @Override
            public void run() {
                acceptLoop();
            }
        }, "monotask-server");
        acceptThread.setDaemon(true);
        acceptThread.start();
    }

    private void acceptLoop() {
        while (!serverSocket.isClosed()) {
            try {
                final Socket client = serverSocket.accept();
                workers.execute(new Runnable() {
                    @Override
                    public void run() {
                        handle(client);
                    }
                });
            } catch (IOException ignored) {
                break;
            }
        }
    }

    private void handle(Socket client) {
        try {
            client.setSoTimeout(5000);
            BufferedInputStream input = new BufferedInputStream(client.getInputStream());
            String requestLine = readLine(input);
            if (requestLine == null) return;

            String[] parts = requestLine.split(" ");
            if (parts.length < 2 || !"GET".equals(parts[0])) {
                writeNotFound(client.getOutputStream());
                return;
            }

            String path = extractPath(parts[1]);
            if (path == null || path.indexOf("..") >= 0) {
                writeNotFound(client.getOutputStream());
                return;
            }
            if (path.length() == 0 || path.equals("/")) {
                path = "/index.html";
            } else if (path.endsWith("/")) {
                path = path + "index.html";
            }

            String relative = path.startsWith("/") ? path.substring(1) : path;
            String assetPath = "mt__" + relative.replace("/", "__");
            InputStream asset;
            try {
                asset = assets.open(assetPath);
            } catch (IOException missing) {
                writeNotFound(client.getOutputStream());
                return;
            }

            ByteArrayOutputStream buffer = new ByteArrayOutputStream();
            byte[] chunk = new byte[8192];
            int read;
            try {
                while ((read = asset.read(chunk)) != -1) {
                    buffer.write(chunk, 0, read);
                }
            } finally {
                asset.close();
            }

            byte[] body = buffer.toByteArray();
            String header = "HTTP/1.1 200 OK\r\n"
                    + "Content-Type: " + mimeType(path) + "\r\n"
                    + "Content-Length: " + body.length + "\r\n"
                    + "Cache-Control: no-store\r\n"
                    + "Connection: close\r\n\r\n";
            OutputStream output = new BufferedOutputStream(client.getOutputStream());
            output.write(header.getBytes("US-ASCII"));
            output.write(body);
            output.flush();
        } catch (SocketTimeoutException ignored) {
        } catch (IOException ignored) {
        } finally {
            try {
                client.close();
            } catch (IOException ignored) {
            }
        }
    }

    private static String readLine(InputStream input) throws IOException {
        ByteArrayOutputStream buffer = new ByteArrayOutputStream();
        int value;
        while ((value = input.read()) != -1) {
            if (value == '\n') break;
            if (value != '\r') buffer.write(value);
            if (buffer.size() > 8192) break;
        }
        if (buffer.size() == 0 && value == -1) return null;
        return buffer.toString("US-ASCII");
    }

    private static String extractPath(String target) {
        try {
            if (target.startsWith("http://") || target.startsWith("https://")) {
                java.net.URI uri = java.net.URI.create(target);
                return uri.getPath();
            }
            int query = target.indexOf('?');
            if (query >= 0) target = target.substring(0, query);
            return URLDecoder.decode(target, "UTF-8");
        } catch (Exception error) {
            return null;
        }
    }

    private static String mimeType(String path) {
        String lower = path.toLowerCase();
        if (lower.endsWith(".html") || lower.endsWith(".htm")) return "text/html; charset=utf-8";
        if (lower.endsWith(".css")) return "text/css; charset=utf-8";
        if (lower.endsWith(".js")) return "application/javascript; charset=utf-8";
        if (lower.endsWith(".webmanifest")) return "application/manifest+json; charset=utf-8";
        if (lower.endsWith(".json")) return "application/json; charset=utf-8";
        if (lower.endsWith(".png")) return "image/png";
        if (lower.endsWith(".svg")) return "image/svg+xml";
        if (lower.endsWith(".ico")) return "image/x-icon";
        if (lower.endsWith(".txt") || lower.endsWith(".md")) return "text/plain; charset=utf-8";
        return "application/octet-stream";
    }

    private static void writeNotFound(OutputStream output) throws IOException {
        byte[] body = "Not found".getBytes("UTF-8");
        String header = "HTTP/1.1 404 Not Found\r\n"
                + "Content-Type: text/plain; charset=utf-8\r\n"
                + "Content-Length: " + body.length + "\r\n"
                + "Connection: close\r\n\r\n";
        BufferedOutputStream buffered = new BufferedOutputStream(output);
        buffered.write(header.getBytes("US-ASCII"));
        buffered.write(body);
        buffered.flush();
    }
}
