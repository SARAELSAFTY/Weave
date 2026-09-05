export default {
  async fetch(request, env) {
    const corsHeaders = {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "POST, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type, Authorization",
      "Access-Control-Max-Age": "86400",
    };

    if (request.method === "OPTIONS") {
      return new Response(null, {
        status: 204,
        headers: corsHeaders,
      });
    }

    if (request.method !== "POST") {
      return new Response(
        JSON.stringify({ error: "Method Not Allowed. Only POST is accepted." }),
        {
          status: 405,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
            "Allow": "POST, OPTIONS",
          },
        }
      );
    }

    const MAX_BODY_BYTES = 20 * 1024;
    const contentLength = request.headers.get("Content-Length");
    if (contentLength && parseInt(contentLength, 10) > MAX_BODY_BYTES) {
      return new Response(
        JSON.stringify({ error: `Payload Too Large. Max allowed size is ${MAX_BODY_BYTES} bytes.` }),
        {
          status: 413,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
          },
        }
      );
    }

    let rawBody;
    try {
      rawBody = await request.arrayBuffer();
    } catch (err) {
      return new Response(
        JSON.stringify({ error: "Failed to read request body." }),
        {
          status: 400,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
          },
        }
      );
    }

    if (rawBody.byteLength > MAX_BODY_BYTES) {
      return new Response(
        JSON.stringify({ error: `Payload Too Large. Max allowed size is ${MAX_BODY_BYTES} bytes.` }),
        {
          status: 413,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
          },
        }
      );
    }

    if (!env.GROQ_API_KEY) {
      return new Response(
        JSON.stringify({ error: "Server misconfiguration: GROQ_API_KEY secret is not set." }),
        {
          status: 500,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
          },
        }
      );
    }

    try {
      const groqResponse = await fetch("https://api.groq.com/openai/v1/chat/completions", {
        method: "POST",
        headers: {
          "Content-Type": request.headers.get("Content-Type") || "application/json",
          "Authorization": `Bearer ${env.GROQ_API_KEY}`,
        },
        body: rawBody,
      });

      const responseHeaders = new Headers(corsHeaders);
      const responseContentType = groqResponse.headers.get("Content-Type");
      if (responseContentType) {
        responseHeaders.set("Content-Type", responseContentType);
      }

      return new Response(groqResponse.body, {
        status: groqResponse.status,
        statusText: groqResponse.statusText,
        headers: responseHeaders,
      });
    } catch (err) {
      return new Response(
        JSON.stringify({ error: `Failed to forward request to Groq: ${err.message}` }),
        {
          status: 502,
          headers: {
            ...corsHeaders,
            "Content-Type": "application/json",
          },
        }
      );
    }
  },
};
