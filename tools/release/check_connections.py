"""Read-only credential checks. Never print credentials or account responses."""
import json
import os
import urllib.error
import urllib.request


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def get(url, headers):
    request = urllib.request.Request(url, headers={**headers, 'User-Agent': 'OctoberJemini-Release-Validation/1.0'})
    try:
        with urllib.request.build_opener(NoRedirect).open(request, timeout=30) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f'Credential check failed with HTTP {error.code}; no response body logged.') from None
    except urllib.error.URLError:
        raise SystemExit('Credential check could not reach the service.') from None


def main():
    target = os.environ.get('DESTINATION', 'validate')
    thunder = os.environ.get('THUNDERSTORE_TOKEN', '').strip()
    nexus = os.environ.get('NEXUSMODS_API_KEY', '').strip()
    if target in ('validate', 'thunderstore', 'both'):
        if thunder:
            user = get('https://thunderstore.io/api/experimental/current-user/', {'Authorization': 'Bearer ' + thunder})
            if not user.get('username') or 'OctoberJemini' not in user.get('teams', []):
                raise SystemExit('Thunderstore token does not identify an OctoberJemini team member.')
            print('Thunderstore: authenticated; OctoberJemini team confirmed.')
        elif target != 'validate':
            raise SystemExit('Thunderstore token missing.')
        else:
            print('Thunderstore: not connected yet.')
    if target in ('validate', 'nexus', 'both'):
        if nexus:
            get('https://api.nexusmods.com/v3/content-settings', {'apikey': nexus})
            print('Nexus: authenticated read-only API request succeeded; response not logged.')
        elif target != 'validate':
            raise SystemExit('Nexus API key missing.')
        else:
            print('Nexus: not connected yet.')


if __name__ == '__main__':
    main()
