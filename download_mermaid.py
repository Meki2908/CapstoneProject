import base64
import urllib.request
import json
import zlib

mermaid_code = """classDiagram
    class DamageDealer {
        <<MonoBehaviour>>
        -float weaponLength
        -float weaponDamage
        -float hitRadius
        -LayerMask targetLayer
        -bool canDealDamage
        -List~GameObject~ hasDealtDamage
        +StartDealDamage()
        +EndDealDamage()
        -ProcessHitTransform(Transform, Vector3)
    }

    class WeaponController {
        <<MonoBehaviour>>
        +GetCurrentWeapon() Weapon
    }

    class WeaponGemManager {
        <<Singleton>>
        +GetDamageMultiplier(WeaponType) float
    }

    class EquipmentManager {
        <<Singleton>>
        +GetTotalCritRateBonus() float
        +GetTotalCritDamageMultiplier() float
    }

    class IDamageable {
        <<Interface>>
        +TakeDamage(int damage, Vector3 hitPoint)
        +GetTransform() Transform
    }

    class PlayerHealth {
        <<MonoBehaviour>>
        +TakeDamage(float damage, Vector3 hitPoint)
    }

    class TakeDamageTest {
        <<Enemy Component>>
        +TakeDamage(float damage, WeaponType weaponType, bool isCrit)
    }

    DamageDealer --> WeaponController : Uses
    DamageDealer --> WeaponGemManager : Calls (Singleton)
    DamageDealer --> EquipmentManager : Calls (Singleton)

    DamageDealer ..> IDamageable : Calls (via interface)
    DamageDealer ..> PlayerHealth : Calls
    DamageDealer ..> TakeDamageTest : Calls
"""

def generate_mermaid_url(mermaid_text):
    # Encode for mermaid.ink
    data = {"code": mermaid_text, "mermaid": {"theme": "default"}}
    json_str = json.dumps(data)
    encoded = base64.b64encode(json_str.encode('utf-8')).decode('utf-8')
    return f"https://mermaid.ink/img/pako:{encoded}"

url = generate_mermaid_url(mermaid_code)
print(f"Downloading from {url}")

req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
with urllib.request.urlopen(req) as response, open("c:/Users/ACER/Documents/CapstoneProject/ClassDiagram_HethongSatThuong.png", "wb") as out_file:
    out_file.write(response.read())

print("Saved to ClassDiagram_HethongSatThuong.png")
