"use client";

import React, { useState, useEffect, useRef } from "react";
import {
  X,
  Send,
  Plus,
  Trash2,
  Clock,
  ChevronLeft,
  Loader2,
  ShoppingBag,
  Check,
} from "lucide-react";
import { ChatSession, ChatMessage } from "@/types/chat";
import shopAiApi from "@/api/shopAiApi";
import { useAuthStore } from "@/stores/authStore";
import { useCartStore } from "@/stores/cartStore";
import ChatMessageContent from "./ChatMessageContent";
import ProductCardMini from "./ProductCardMini";
import InteractiveOrderCard from "./InteractiveOrderCard";
import OrderSuccessCard from "./OrderSuccessCard";
import OrderTrackingCard from "./OrderTrackingCard";

export default function ChatbotWidget() {
  const { isAuthenticated, initAuth } = useAuthStore();
  const { addMultipleItems } = useCartStore();

  const [isOpen, setIsOpen] = useState(false);
  const [showSessions, setShowSessions] = useState(false);
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [currentSessionId, setCurrentSessionId] = useState<number | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [sessionToken, setSessionToken] = useState<string>("");
  const [addedAllMsgId, setAddedAllMsgId] = useState<number | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Helper to ensure payload is parsed if it came as a JSON string
  const parsePayload = (payload: any) => {
    if (!payload) return null;
    if (typeof payload === "string") {
      try {
        return JSON.parse(payload);
      } catch {
        return payload;
      }
    }
    return payload;
  };

  // 0. Khởi tạo xác thực nếu chưa chạy
  useEffect(() => {
    initAuth();
  }, [initAuth]);

  // 1. Khởi tạo SessionToken từ localStorage
  useEffect(() => {
    if (typeof window !== "undefined") {
      let token = localStorage.getItem("solaris_chat_token");
      if (!token) {
        token = "usr_" + Math.random().toString(36).substring(2, 15);
        localStorage.setItem("solaris_chat_token", token);
      }
      setSessionToken(token);
    }
  }, []);

  // 2. Tải danh sách phiên chat khi mở widget (hỗ trợ cả khách vãng lai và thành viên)
  useEffect(() => {
    if (isOpen && sessionToken) {
      loadSessions();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, sessionToken]);

  // 3. Tự động cuộn xuống tin nhắn mới nhất
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isLoading]);

  const loadSessions = async () => {
    try {
      const list = await shopAiApi.getSessions(sessionToken);
      setSessions(list || []);
      if (list && list.length > 0 && !currentSessionId) {
        selectSession(list[0].id);
      } else if (!currentSessionId) {
        handleNewSession();
      }
    } catch (error) {
      console.error("Lỗi tải danh sách phiên chat:", error);
    }
  };

  const selectSession = async (sessionId: number) => {
    try {
      setCurrentSessionId(sessionId);
      setShowSessions(false);
      const msgList = await shopAiApi.getSessionMessages(
        sessionId,
        sessionToken,
      );
      const parsedList = (msgList || []).map((m: any) => ({
        ...m,
        payload: parsePayload(m.payload),
      }));
      setMessages(parsedList);
    } catch (error) {
      console.error("Lỗi tải tin nhắn phiên chat:", error);
    }
  };

  const handleNewSession = async () => {
    try {
      setIsLoading(true);
      const newSess = await shopAiApi.createSession({
        sessionToken,
        title: "Cuộc trò chuyện mới",
      });
      setSessions((prev) => [newSess, ...prev]);
      setCurrentSessionId(newSess.id);
      setMessages([
        {
          id: 0,
          role: "model",
          content:
            "Dạ Solaris AI xin chào bạn!\nEm hỗ trợ thông tin **nông sản sạch VietGAP**, **tra cứu tiến độ giao hàng xe lạnh TMS (2°C - 8°C)**, **chương trình khuyến mãi**, hoặc **lên đơn đặt hàng trực tiếp** theo dữ liệu thực tế tại đây ạ!",
          payloadType: "none",
          createdAt: new Date().toISOString(),
        },
      ]);
      setShowSessions(false);
    } catch (error) {
      console.error("Lỗi tạo phiên mới:", error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteSession = async (
    sessionId: number,
    e: React.MouseEvent,
  ) => {
    e.stopPropagation();
    try {
      await shopAiApi.deleteSession(sessionId, sessionToken);
      setSessions((prev) => prev.filter((s) => s.id !== sessionId));
      if (currentSessionId === sessionId) {
        handleNewSession();
      }
    } catch (error) {
      console.error("Lỗi xóa phiên chat:", error);
    }
  };

  // 4. Gửi tin nhắn
  const handleSendMessage = async (textToSend?: string) => {
    const message = (textToSend || inputMessage).trim();
    if (!message || isLoading) return;

    setInputMessage("");

    const tempUserMsg: ChatMessage = {
      id: Date.now(),
      role: "user",
      content: message,
      payloadType: "none",
      createdAt: new Date().toISOString(),
    };
    setMessages((prev) => [...prev, tempUserMsg]);
    setIsLoading(true);

    try {
      const res = await shopAiApi.sendMessage({
        sessionId: currentSessionId || undefined,
        sessionToken,
        message,
      });

      if (!currentSessionId && res.sessionId) {
        setCurrentSessionId(res.sessionId);
      }

      const modelMsg: ChatMessage = {
        id: res.messageId || Date.now() + 1,
        role: "model",
        content: res.content,
        payloadType: res.payloadType as any,
        payload: parsePayload(res.payload),
        createdAt: res.createdAt || new Date().toISOString(),
      };
      setMessages((prev) => [...prev, modelMsg]);

      if (res.title) {
        setSessions((prev) =>
          prev.map((s) =>
            s.id === res.sessionId ? { ...s, title: res.title } : s,
          ),
        );
      }
    } catch (error) {
      console.error("Lỗi gửi tin nhắn:", error);
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 2,
          role: "model",
          content:
            "Dạ xin lỗi bạn, đường truyền kết nối AI đang bận một chút. Bạn vui lòng thử nhắn lại giúp em nhé!",
          payloadType: "none",
          createdAt: new Date().toISOString(),
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  // 1-Click Add All Products to Cart
  const handleAddAllToCart = async (msgId: number, products: any[]) => {
    try {
      const items = products.map((p: any) => ({
        variantId: p.variantId,
        uoMId: p.uoMId || 1,
        quantity: 1,
      }));
      await addMultipleItems(items);
      setAddedAllMsgId(msgId);
      setTimeout(() => setAddedAllMsgId(null), 2500);
    } catch (err) {
      console.error("Lỗi thêm tất cả vào giỏ:", err);
    }
  };

  // Dynamic Quick Chips based on Auth state
  const quickPrompts = isAuthenticated
    ? [
        "🔄 Đặt lại đơn hàng cũ",
        "🚚 Kiểm tra đơn hàng của tôi",
        "🥑 Bơ Sáp 034 & Sầu Riêng",
        "🎁 Khuyến mãi đang chạy",
        "📦 Chính sách Freeship 300k",
      ]
    : [
        "🥑 Nông sản tươi hôm nay",
        "🥑 Bơ Sáp 034 & Sầu Riêng",
        "🎁 Khuyến mãi đang chạy",
        "📦 Chính sách Freeship 300k",
        "🚚 Tra cứu đơn hàng ORD-...",
      ];

  // Check if any interactive order exists to expand widget horizontally (x1.5 width)
  const hasInteractiveOrder = messages.some(
    (m) => m.payloadType === "interactive_order",
  );

  return (
    <div className="fixed bottom-6 right-6 z-50">
      {/* Minimalist Round AI Toggle Button */}
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className={`group relative w-14 h-14 bg-gradient-to-tr from-emerald-700 via-emerald-600 to-teal-500 rounded-full shadow-2xl shadow-emerald-700/30 hover:shadow-emerald-600/50 hover:scale-108 active:scale-95 transition-all duration-300 flex items-center justify-center cursor-pointer border-2 border-white/20 ${
          isOpen
            ? "scale-0 opacity-0 pointer-events-none"
            : "scale-100 opacity-100"
        }`}
        title="Trợ Lý Nông Sản AI (3.5 Flash)"
        aria-label="Trợ Lý Nông Sản AI"
      >
        {/* Pulse ring */}
        <span className="absolute -inset-0.5 rounded-full bg-emerald-400/40 animate-ping pointer-events-none opacity-70"></span>

        {/* AI Typography Icon */}
        <span className="relative flex items-center justify-center font-black text-lg tracking-wider text-white select-none">
          AI
        </span>

        {/* Accessible text for screen readers & tests */}
        <span className="sr-only">Trợ Lý Nông Sản AI</span>
        <span className="sr-only">3.5 Flash</span>

        {/* Online pulsing dot */}
        <span className="absolute top-0.5 right-0.5 flex h-3.5 w-3.5">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-300 opacity-75"></span>
          <span className="relative inline-flex rounded-full h-3.5 w-3.5 bg-emerald-400 border-2 border-white"></span>
        </span>
      </button>

      {/* Chat Modal Window with Smooth Transition & Auto Expansion */}
      <div
        className={`transition-all duration-300 ease-out origin-bottom-right ${
          isOpen
            ? "scale-100 opacity-100 translate-y-0 pointer-events-auto"
            : "scale-75 opacity-0 translate-y-8 pointer-events-none absolute bottom-0 right-0"
        }`}
      >
        <div
          className={`h-[590px] bg-white rounded-3xl shadow-2xl border border-emerald-100 flex flex-col overflow-hidden transition-[width] duration-300 ease-in-out ${
            hasInteractiveOrder
              ? "w-[360px] sm:w-[620px] lg:w-[650px]"
              : "w-[360px] sm:w-[420px]"
          }`}
        >
          {/* Header */}
          <div className="bg-gradient-to-r from-emerald-600 to-teal-700 p-3.5 text-white flex items-center justify-between shadow-md">
            <div className="flex items-center gap-2.5">
              {showSessions ? (
                <button
                  onClick={() => setShowSessions(false)}
                  className="p-1 hover:bg-white/10 rounded-full transition-colors cursor-pointer"
                  title="Quay lại khung chat"
                >
                  <ChevronLeft className="w-5 h-5" />
                </button>
              ) : (
                <button
                  onClick={() => setShowSessions(true)}
                  className="p-1 hover:bg-white/10 rounded-lg transition-colors flex items-center gap-1 text-[11px] font-semibold bg-white/10 px-2 py-1 cursor-pointer"
                  title="Lịch sử cuộc hội thoại"
                >
                  <Clock className="w-3.5 h-3.5" />
                  <span>Lịch sử</span>
                </button>
              )}

              <div>
                <h3 className="font-bold text-xs leading-tight flex items-center gap-1.5">
                  Solaris AI Commerce
                  <span className="px-1.5 py-0.2 bg-emerald-400/30 text-[9px] font-bold rounded text-emerald-100">
                    Online
                  </span>
                </h3>
                <p className="text-[10px] text-emerald-100/90 flex items-center gap-1 mt-0.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-300 animate-pulse"></span>
                  Tư vấn & Lên đơn trực tiếp
                </p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              <button
                data-testid="ai-new-session-btn"
                onClick={handleNewSession}
                className="p-1.5 text-white/80 hover:text-white hover:bg-white/10 rounded-full transition-colors cursor-pointer"
                title="Bắt đầu đoạn chat mới"
              >
                <Plus className="w-4 h-4" />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                className="p-1.5 text-white/80 hover:text-white hover:bg-white/10 rounded-full transition-colors cursor-pointer"
                title="Đóng khung chat"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
          </div>

          {/* Chat Body OR Sessions List */}
          {showSessions ? (
            <div className="flex-1 p-3 overflow-y-auto bg-slate-50 space-y-2 text-xs">
              <div className="flex items-center justify-between pb-2 border-b border-slate-200">
                <span className="font-bold text-slate-700 text-xs">
                  Lịch sử các cuộc hội thoại
                </span>
                <button
                  onClick={handleNewSession}
                  className="flex items-center gap-1 text-emerald-700 font-bold hover:underline text-[11px] cursor-pointer"
                >
                  <Plus className="w-3.5 h-3.5" /> Chat mới
                </button>
              </div>

              {sessions.length === 0 ? (
                <p className="text-center text-slate-400 py-8">
                  Chưa có cuộc trò chuyện nào.
                </p>
              ) : (
                sessions.map((s) => (
                  <div
                    key={s.id}
                    onClick={() => selectSession(s.id)}
                    className={`p-2.5 rounded-xl border transition-all cursor-pointer flex items-center justify-between ${
                      currentSessionId === s.id
                        ? "bg-emerald-50 border-emerald-300 text-emerald-900 font-bold shadow-xs"
                        : "bg-white border-slate-200 hover:border-emerald-200 text-slate-700"
                    }`}
                  >
                    <div className="min-w-0 flex-1 pr-2">
                      <p className="truncate text-xs font-semibold">
                        {s.title || "Cuộc trò chuyện"}
                      </p>
                      <p className="text-[10px] text-slate-400 mt-0.5">
                        {new Date(s.updatedAt).toLocaleDateString("vi-VN")} •{" "}
                        {s.totalMessages} tin nhắn
                      </p>
                    </div>

                    <button
                      onClick={(e) => handleDeleteSession(s.id, e)}
                      className="p-1 text-slate-300 hover:text-rose-500 rounded transition-colors cursor-pointer"
                      title="Xóa phiên này"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                ))
              )}
            </div>
          ) : (
            <div className="flex-1 p-3.5 overflow-y-auto space-y-3.5 bg-slate-50 text-xs">
              {/* Messages Stream */}
              {messages.map((m, idx) => (
                <div
                  key={m.id || idx}
                  className={`flex items-start gap-2 ${m.role === "user" ? "justify-end" : "justify-start"}`}
                >
                  {m.role === "model" && (
                    <div className="w-6 h-6 rounded-full bg-emerald-600 text-white flex items-center justify-center text-[10px] shrink-0 font-bold shadow-xs select-none">
                      AI
                    </div>
                  )}

                  <div
                    className={`max-w-[88%] space-y-2.5 ${m.role === "user" ? "items-end" : "items-start"}`}
                  >
                    {/* Main Message Bubble */}
                    <div
                      className={`p-3.5 rounded-2xl leading-relaxed ${
                        m.role === "user"
                          ? "bg-emerald-600 text-white rounded-tr-xs shadow-xs font-medium"
                          : "bg-white text-slate-800 rounded-tl-xs shadow-2xs border border-slate-200/70"
                      }`}
                    >
                      <ChatMessageContent
                        content={m.content}
                        isUser={m.role === "user"}
                      />
                    </div>

                    {/* Render Payload: Product Cards with 1-Click Add All */}
                    {m.payloadType === "product_cards" &&
                      Array.isArray(m.payload) &&
                      m.payload.length > 0 && (
                        <div className="space-y-2 w-full pt-1">
                          {m.payload.length >= 2 && (
                            <div className="flex items-center justify-between px-1 pb-1">
                              <span className="text-[10px] text-slate-500 font-medium">
                                Gợi ý {m.payload.length} nông sản sạch:
                              </span>
                              <button
                                type="button"
                                onClick={() =>
                                  handleAddAllToCart(m.id, m.payload)
                                }
                                className={`text-[10px] font-bold px-2.5 py-1 rounded-lg transition-all flex items-center gap-1.5 shadow-2xs cursor-pointer ${
                                  addedAllMsgId === m.id
                                    ? "bg-emerald-600 text-white"
                                    : "bg-emerald-50 text-emerald-700 hover:bg-emerald-600 hover:text-white border border-emerald-200"
                                }`}
                              >
                                {addedAllMsgId === m.id ? (
                                  <>
                                    <Check className="w-3 h-3" />
                                    <span>Đã thêm tất cả</span>
                                  </>
                                ) : (
                                  <>
                                    <ShoppingBag className="w-3 h-3" />
                                    <span>Thêm tất cả vào giỏ</span>
                                  </>
                                )}
                              </button>
                            </div>
                          )}
                          {m.payload.map((prod: any, pIdx: number) => (
                            <ProductCardMini
                              key={
                                prod.id ||
                                prod.variantId ||
                                prod.slug ||
                                `prod-${pIdx}`
                              }
                              product={prod}
                            />
                          ))}
                        </div>
                      )}

                    {/* Render Payload: Interactive Order Form */}
                    {m.payloadType === "interactive_order" && m.payload && (
                      <div className="w-full pt-1">
                        <InteractiveOrderCard
                          sessionId={currentSessionId || 0}
                          payload={m.payload}
                          onOrderSuccess={() => {
                            // Handled via redirect
                          }}
                        />
                      </div>
                    )}

                    {/* Render Payload: Order Success */}
                    {m.payloadType === "order_success" && m.payload && (
                      <div className="w-full pt-1">
                        <OrderSuccessCard payload={m.payload} />
                      </div>
                    )}

                    {/* Render Payload: Order Tracking */}
                    {m.payloadType === "order_tracking" && m.payload && (
                      <div className="w-full pt-1">
                        <OrderTrackingCard tracking={m.payload} />
                      </div>
                    )}
                  </div>
                </div>
              ))}

              {/* Loading Indicator */}
              {isLoading && (
                <div className="flex items-center gap-2 text-slate-400 text-xs pl-8">
                  <Loader2 className="w-3.5 h-3.5 animate-spin text-emerald-600" />
                  <span>Solaris AI đang tra cứu dữ liệu...</span>
                </div>
              )}

              <div ref={messagesEndRef} />
            </div>
          )}

          {/* Dynamic Suggestion Chips */}
          {!showSessions && (
            <div className="px-3 py-1.5 bg-slate-50 border-t border-slate-100 flex items-center gap-1.5 overflow-x-auto no-scrollbar">
              {quickPrompts.map((prompt, i) => (
                <button
                  key={`prompt-${i}-${prompt}`}
                  onClick={() => handleSendMessage(prompt)}
                  className="whitespace-nowrap px-2.5 py-1 bg-white hover:bg-emerald-50 hover:border-emerald-300 border border-slate-200 rounded-full text-[10px] text-slate-600 hover:text-emerald-700 transition-all shrink-0 font-medium cursor-pointer"
                >
                  {prompt}
                </button>
              ))}
            </div>
          )}

          {/* Input Bar */}
          {!showSessions && (
            <div className="p-3 bg-white border-t border-slate-100">
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  const inputEl = e.currentTarget.querySelector("input");
                  const text = inputEl?.value || inputMessage;
                  handleSendMessage(text);
                }}
                className="flex items-center gap-2"
              >
                <input
                  data-testid="ai-chat-input"
                  type="text"
                  placeholder="Hỏi AI hoặc gõ 'Mua 2kg bơ 034'..."
                  value={inputMessage}
                  onChange={(e) => setInputMessage(e.target.value)}
                  disabled={isLoading}
                  className="flex-1 px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-full text-xs focus:outline-hidden focus:ring-2 focus:ring-emerald-500 focus:bg-white disabled:opacity-50"
                />
                <button
                  data-testid="ai-chat-send-btn"
                  type="submit"
                  disabled={isLoading || !inputMessage.trim()}
                  className="p-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-full transition-colors shadow-xs disabled:opacity-40 cursor-pointer"
                >
                  <Send className="w-4 h-4" />
                </button>
              </form>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
