// Cloudflare Worker: HTTPS relay for Dreamlo (dreamlo.com).
//
// Why this exists: Dreamlo's free tier only serves http://, not https://
// ("ERROR:SSL not enabled for this leaderboard." - confirmed with curl).
// A page hosted on itch.io is served over https, and browsers block an
// https page from making plain http requests ("mixed content") - so the
// game can't talk to Dreamlo directly from a WebGL build. This Worker sits
// in front of Dreamlo: the game calls this Worker over https, the Worker
// calls Dreamlo over http server-side (not subject to any browser rules),
// and relays the response back over https. It also adds a permissive CORS
// header so the browser's cross-origin check passes too.
//
// Deploy (Cloudflare dashboard, free tier, no credit card required):
//   1. workers.cloudflare.com -> sign up / log in -> "Create Worker"
//   2. Delete the placeholder code, paste this whole file in, click "Deploy"
//   3. Copy the resulting URL (looks like https://<name>.<subdomain>.workers.dev)
//   4. Give that URL to Claude - it goes into LeaderboardManager's
//      "Proxy Base Url" field, and everything else keeps working unchanged.
//
// This Worker is a dumb path-forwarder - whatever path/query hits this
// Worker gets replayed against http://dreamlo.com unchanged. So
//   https://<your-worker>.workers.dev/lb/<privatecode>/add/Name/100
// becomes
//   http://dreamlo.com/lb/<privatecode>/add/Name/100
// and the response is passed straight back.

export default {
  async fetch(request) {
    const url = new URL(request.url);
    const target = "http://dreamlo.com" + url.pathname + url.search;

    const dreamloResponse = await fetch(target);
    const body = await dreamloResponse.text();

    return new Response(body, {
      status: dreamloResponse.status,
      headers: {
        "Content-Type": "text/plain",
        "Access-Control-Allow-Origin": "*",
      },
    });
  },
};
