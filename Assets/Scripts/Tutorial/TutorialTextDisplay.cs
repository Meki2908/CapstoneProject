using UnityEngine;
using TMPro;  // Để sử dụng TextMeshPro

public class TutorialTextDisplay : MonoBehaviour
{
    public TMP_Text tutorialText;  // Tham chiếu đến TMP_Text UI
    private string[] tutorialSteps;  // Các bước nhiệm vụ
    private int currentStep = 0;  // Để theo dõi bước hiện tại

    void Start()
    {
        // Các nhiệm vụ sẽ xuất hiện lần lượt
       tutorialSteps = new string[]
{
    "Press   Space   to   jump",  // Task 1: Jump
    "Press   right   mouse   button   to   roll",  // Task 2: Roll
    "Press   left   mouse   button   to   attack",  // Task 3: Attack
    "Press   E   on   the   dummy   to   use   the   first   special   skill",  // Task 4: Special skill E
    "Press   R   on   the   dummy   to   use   the   second   special   skill",  // Task 5: Special skill R
    "Press   T   on   the   dummy   to   use   the   third   special   skill ",  // Task 6: Special skill T
    "Press   Q   to   use   the   ultimate   skill",  // Task 7: Ultimate skill
    "You   have   completed   the   tutorial!"  // Completion message
};

        // Hiển thị nhiệm vụ đầu tiên ngay mà không thay đổi currentStep
        tutorialText.text = tutorialSteps[currentStep];

    }

    void Update()
    {
        // Kiểm tra các thao tác người chơi có thể thực hiện

        // Nhiệm vụ 1: Space để nhảy
        if (currentStep == 0 && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 2: Chuột phải để lăn
        if (currentStep == 1 && Input.GetMouseButtonDown(1))  // Chuột phải (Button 1) để lăn
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 3: Chuột trái để đánh thường
        if (currentStep == 2 && Input.GetMouseButtonDown(0))  // Chuột trái (Button 0) để đánh thường
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 4: Ấn E để sử dụng skill đặc biệt thứ nhất
        if (currentStep == 3 && Input.GetKeyDown(KeyCode.E))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 5: Ấn R để sử dụng skill đặc biệt thứ hai
        if (currentStep == 4 && Input.GetKeyDown(KeyCode.R))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 6: Ấn T để sử dụng skill đặc biệt thứ ba
        if (currentStep == 5 && Input.GetKeyDown(KeyCode.T))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }

        // Nhiệm vụ 7: Ấn Q để sử dụng chiêu cuối
        if (currentStep == 6 && Input.GetKeyDown(KeyCode.Q))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }
        if (currentStep == 7 && Input.GetKeyDown(KeyCode.O))
        {
            ShowNextTutorialText();  // Chuyển qua nhiệm vụ tiếp theo
        }
    }

    // Hàm này sẽ hiển thị từng nhiệm vụ khi người chơi hoàn thành
    public void ShowNextTutorialText()
    {
        // Nếu còn nhiệm vụ, tiếp tục
        if (currentStep < tutorialSteps.Length)
        {
            currentStep++;  // Tăng bước lên sau khi chuyển sang nhiệm vụ tiếp theo
            tutorialText.text = tutorialSteps[currentStep];  // Cập nhật TMP_Text với nhiệm vụ tiếp theo
        }
        else
        {
            // Nếu không còn nhiệm vụ nào, hiển thị thông báo hoàn thành
            tutorialText.text = "Bạn đã hoàn thành tutorial!";
        }
    }
}