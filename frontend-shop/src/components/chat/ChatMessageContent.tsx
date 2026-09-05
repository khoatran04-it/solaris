"use client";

import React from "react";

interface ChatMessageContentProps {
  content: string;
  isUser?: boolean;
}

/**
 * Trình render Markdown tối giản, an toàn và không phụ thuộc thư viện nặng.
 * Hỗ trợ: Bold (**text**), Italic (*text*), Heading (### ), Lists (- / * / 1.), Inline Code (`code`), Links ([text](url)).
 */
export default function ChatMessageContent({
  content,
  isUser = false,
}: ChatMessageContentProps) {
  if (isUser) {
    return <p className="whitespace-pre-wrap leading-relaxed">{content}</p>;
  }

  // Tách các dòng
  const lines = content.split("\n");
  const elements: React.ReactNode[] = [];
  let currentList: { type: "ul" | "ol"; items: string[] } | null = null;

  const flushList = (key: string) => {
    if (!currentList) return;
    if (currentList.type === "ul") {
      elements.push(
        <ul
          key={key}
          className="space-y-1.5 my-1.5 pl-4 list-disc marker:text-emerald-600"
        >
          {currentList.items.map((item, idx) => (
            <li key={idx} className="leading-relaxed">
              {parseInlineFormatting(item)}
            </li>
          ))}
        </ul>,
      );
    } else {
      elements.push(
        <ol
          key={key}
          className="space-y-1.5 my-1.5 pl-4 list-decimal marker:text-emerald-600 marker:font-bold"
        >
          {currentList.items.map((item, idx) => (
            <li key={idx} className="leading-relaxed">
              {parseInlineFormatting(item)}
            </li>
          ))}
        </ol>,
      );
    }
    currentList = null;
  };

  lines.forEach((line, index) => {
    const trimmed = line.trim();

    // Kiểm tra dòng trống
    if (!trimmed) {
      flushList(`list-before-empty-${index}`);
      return;
    }

    // Kiểm tra Bullet List (- item hoặc * item)
    const bulletMatch = line.match(/^(\s*)[-*]\s+(.+)$/);
    if (bulletMatch) {
      if (!currentList || currentList.type !== "ul") {
        flushList(`list-flush-${index}`);
        currentList = { type: "ul", items: [] };
      }
      currentList.items.push(bulletMatch[2]);
      return;
    }

    // Kiểm tra Numbered List (1. item)
    const numberMatch = line.match(/^(\s*)\d+\.\s+(.+)$/);
    if (numberMatch) {
      if (!currentList || currentList.type !== "ol") {
        flushList(`list-flush-${index}`);
        currentList = { type: "ol", items: [] };
      }
      currentList.items.push(numberMatch[2]);
      return;
    }

    // Nếu không phải list thì flush list trước đó nếu có
    flushList(`list-flush-${index}`);

    // Kiểm tra Headings (### , ## , # )
    if (trimmed.startsWith("### ")) {
      elements.push(
        <h4
          key={`h4-${index}`}
          className="font-extrabold text-slate-900 text-xs mt-2 mb-1 tracking-tight"
        >
          {parseInlineFormatting(trimmed.substring(4))}
        </h4>,
      );
      return;
    }
    if (trimmed.startsWith("## ")) {
      elements.push(
        <h3
          key={`h3-${index}`}
          className="font-extrabold text-slate-900 text-sm mt-2.5 mb-1 tracking-tight"
        >
          {parseInlineFormatting(trimmed.substring(3))}
        </h3>,
      );
      return;
    }
    if (trimmed.startsWith("# ")) {
      elements.push(
        <h2
          key={`h2-${index}`}
          className="font-black text-slate-900 text-base mt-3 mb-1 tracking-tight"
        >
          {parseInlineFormatting(trimmed.substring(2))}
        </h2>,
      );
      return;
    }

    // Đoạn văn thông thường
    elements.push(
      <p key={`p-${index}`} className="leading-relaxed mb-1.5 last:mb-0">
        {parseInlineFormatting(line)}
      </p>,
    );
  });

  // Flush list còn lại nếu có ở cuối
  flushList("list-end");

  return <div className="space-y-1 text-xs">{elements}</div>;
}

/**
 * Xử lý định dạng inline: **bold**, *italic*, `code`, [link](url)
 */
function parseInlineFormatting(text: string): React.ReactNode[] {
  const tokens: React.ReactNode[] = [];
  // Regex bắt: **bold** | *italic* | `code` | [text](url)
  const regex = /(\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;

  while ((match = regex.exec(text)) !== null) {
    // Text trước match
    if (match.index > lastIndex) {
      tokens.push(text.substring(lastIndex, match.index));
    }

    const tokenStr = match[0];

    // Bold: **text**
    if (tokenStr.startsWith("**") && tokenStr.endsWith("**")) {
      const inner = tokenStr.substring(2, tokenStr.length - 2);
      tokens.push(
        <strong key={match.index} className="font-bold text-slate-900">
          {inner}
        </strong>,
      );
    }
    // Italic: *text*
    else if (tokenStr.startsWith("*") && tokenStr.endsWith("*")) {
      const inner = tokenStr.substring(1, tokenStr.length - 1);
      tokens.push(
        <em key={match.index} className="italic text-slate-700">
          {inner}
        </em>,
      );
    }
    // Inline code: `code`
    else if (tokenStr.startsWith("`") && tokenStr.endsWith("`")) {
      const inner = tokenStr.substring(1, tokenStr.length - 1);
      tokens.push(
        <code
          key={match.index}
          className="px-1.5 py-0.5 bg-slate-100 text-emerald-800 rounded font-mono text-[11px]"
        >
          {inner}
        </code>,
      );
    }
    // Link: [label](url)
    else if (
      tokenStr.startsWith("[") &&
      tokenStr.includes("](") &&
      tokenStr.endsWith(")")
    ) {
      const closeBracketIdx = tokenStr.indexOf("](");
      const label = tokenStr.substring(1, closeBracketIdx);
      const url = tokenStr.substring(closeBracketIdx + 2, tokenStr.length - 1);
      tokens.push(
        <a
          key={match.index}
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          className="text-emerald-700 hover:text-emerald-800 font-bold underline underline-offset-2"
        >
          {label}
        </a>,
      );
    } else {
      tokens.push(tokenStr);
    }

    lastIndex = regex.lastIndex;
  }

  // Text còn lại sau match cuối cùng
  if (lastIndex < text.length) {
    tokens.push(text.substring(lastIndex));
  }

  return tokens;
}
