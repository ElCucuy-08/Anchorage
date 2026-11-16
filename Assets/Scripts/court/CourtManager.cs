using UnityEngine;
using TMPro;

public class CourtManager : MonoBehaviour
{
    [System.Serializable]
    public class CourtQuestion
    {
        public string question;
        public string answerA;
        public string answerB;
        public string answerC;
    }

    [Header("Текст вопроса")]
    public TMP_Text questionText;

    [Header("Текст внутри кнопок")]
    public TMP_Text answerAText;
    public TMP_Text answerBText;
    public TMP_Text answerCText;

    private CourtQuestion[] questions;

    private int currentQuestion = 0;
    private int totalPoints = 0;

    private void Start()
    {
        questions = new CourtQuestion[]
        {
            new CourtQuestion
            {
                question = "Подсудимый, вы въехали в США по поддельной грин-карте. Вы признаете свою вину в нелегальном пересечении границы?",
                answerA = "Ваша граница дыра полная, а говорите страна в безопасности, пендосы",
                answerB = "Да, Ваша честь. Я бежал от безысходности и купил документ от отчаяния, чтобы просто выжить",
                answerC = "Я не изготавливал документ. К тому же таможенники изъяли карту без присутствия понятых и оформления протокола"
            },

            new CourtQuestion
            {
                question = "В вашем досье указано, что вы отбыли 6 лет в русской тюрьме за крупную кражу. Почему суд Аляски должен верить, что вы приехали сюда не за тем же?",
                answerA = "А это, уважаемый судья, вас ебать не должно",
                answerB = "Тот срок был главной ошибкой моей жизни. Я выплатил долг обществу и хотел начать всё с чистого листа",
                answerC = "Сведения о судимости переданы с нарушением регламента Интерпола. Эти данные не приобщены к делу официально"
            },

            new CourtQuestion
            {
                question = "Спустя пару недель после прилёта вы вскрыли сейф. Зачем вы снова пошли на преступление?",
                answerA = "Замки лучше ставьте, пендосы. В России такой сейф каждый школьник откроет",
                answerB = "У меня вытащили все сбережения, я замерзал на улице без гроша. Это был глупый акт отчаяния, я готов возместить ущерб",
                answerC = "Полиция вошла в здание на 40 минут раньше, чем ордер был подписан. Это прямое нарушение 4-й поправки"
            },

            new CourtQuestion
            {
                question = "Прокурор утверждает, что вы вскрыли сейф по наводке русской мафии и являетесь членом местного синдиката. Это правда?",
                answerA = "Да хоть главой всей мафии меня зазовите. Мне на ваши американские синдикаты плевать",
                answerB = "Всегда работал только на себя. Никакой мафии нет, я действовал в одиночку из-за голода",
                answerC = "Обвинение строится на показаниях анонимного осведомителя. По закону я имею право на очную ставку, которой не было"
            },

            new CourtQuestion
            {
                question = "Вам предоставляется последнее слово перед вынесением приговора.",
                answerA = "Давайте ваш максимальный срок. Я в русской тюрьме 6 лет отмотал и вашу Аляску пережую",
                answerB = "Я виновен в краже и нелегальном въезде, но я не опасный рецидивист. Дайте мне шанс искупить вину",
                answerC = "Передаю слово адвокату. На основании собранных нарушений мы требуем аннулировать все улики"
            }
        };

        ShowQuestion();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ChooseA();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ChooseB();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ChooseC();
    }

    private void ShowQuestion()
    {
        if (currentQuestion >= questions.Length)
        {
            EndCourt();
            return;
        }

        questionText.text = questions[currentQuestion].question;

        answerAText.text = questions[currentQuestion].answerA;
        answerBText.text = questions[currentQuestion].answerB;
        answerCText.text = questions[currentQuestion].answerC;
    }

    public void ChooseA()
    {
        totalPoints += 1;
        NextQuestion();
    }

    public void ChooseB()
    {
        totalPoints += 2;
        NextQuestion();
    }

    public void ChooseC()
    {
        totalPoints += 3;
        NextQuestion();
    }

    private void NextQuestion()
    {
        currentQuestion++;
        ShowQuestion();
    }

    private void EndCourt()
    {
        Debug.Log("Баллы: " + totalPoints);

        // Секретная концовка
        if (totalPoints == 15)
        {
            Debug.Log("СЕКРЕТНАЯ КОНЦОВКА");
            return;
        }

        // Хардкор
        if (totalPoints <= 7)
        {
            Debug.Log("КОНЦОВКА A - 15 ЛЕТ");
            return;
        }

        // Обычная
        Debug.Log("КОНЦОВКА B - 3 ГОДА");
    }
}