
var destroyTime:float=5;
var audio1: AudioClip;
function Start () {
GetComponent.<AudioSource>().PlayOneShot (audio1);
Destroy (gameObject, destroyTime);
}
function Update () {
}