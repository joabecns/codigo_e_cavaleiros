"""Banco de questões: carrega o JSON, valida e sorteia sem repetir."""
import json
import random
from dataclasses import dataclass


@dataclass
class Question:
    id: str
    topic: str
    text: str
    options: list
    answer: int
    explanation: str = ""

    def shuffled(self, rng=random):
        """Cópia com alternativas embaralhadas (a posição da correta muda)."""
        order = list(range(len(self.options)))
        rng.shuffle(order)
        return Question(self.id, self.topic, self.text,
                        [self.options[i] for i in order],
                        order.index(self.answer), self.explanation)


class QuestionBank:
    def __init__(self, path, rng=None):
        self.rng = rng or random
        self.questions = self._load(path)
        self._used = set()

    @staticmethod
    def _load(path):
        with open(path, encoding="utf-8") as f:
            raw = json.load(f)
        questions = []
        for item in raw:
            q = Question(item["id"], item["topic"], item["text"], list(item["options"]),
                         int(item["answer"]), item.get("explanation", ""))
            if len(q.options) != 4 or not 0 <= q.answer < 4:
                raise ValueError(f"Questão inválida: {q.id}")
            questions.append(q)
        return questions

    def draw(self, topics):
        """Sorteia uma questão dos tópicos indicados, sem repetir até esgotar o grupo."""
        pool = [q for q in self.questions if q.topic in topics]
        if not pool:
            raise ValueError(f"Nenhuma questão para os tópicos {topics}")
        fresh = [q for q in pool if q.id not in self._used]
        if not fresh:
            self._used -= {q.id for q in pool}
            fresh = pool
        choice = self.rng.choice(fresh)
        self._used.add(choice.id)
        return choice.shuffled(self.rng)
