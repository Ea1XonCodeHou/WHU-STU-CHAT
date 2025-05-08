<template>
  <div class="ai-chat-container">
    <div class="chat-header">
      <div class="header-left">
        <router-link to="/home" class="back-button">
          <i class="back-icon"></i>
          返回主页
        </router-link>
      </div>
      <div class="header-title">
        <div class="ai-avatar">AI</div>
        <h1>智能助手</h1>
      </div>
      <div class="header-right">
        <button class="clear-button" @click="confirmClearHistory">
          <i class="clear-icon"></i>
          清空对话
        </button>
      </div>
    </div>
    
    <div class="chat-content" ref="chatContent">
      <!-- 欢迎消息 -->
      <div v-if="messages.length === 0" class="welcome-container">
        <div class="welcome-header">
          <div class="welcome-avatar">AI</div>
          <h2>武汉大学智能助手</h2>
        </div>
        <div class="welcome-message">
          <p>你好，我是武汉大学的智能助手。我可以回答你关于武汉大学的问题，也可以帮助你解决学习和生活中的困惑。</p>
        </div>
        <div class="suggestions">
          <div class="suggestion-title">你可以问我这些问题：</div>
          <div class="suggestion-items">
            <div class="suggestion-item" v-for="(item, index) in suggestions" :key="index" @click="useQuestion(item)">
              {{ item }}
            </div>
          </div>
        </div>
      </div>
      
      <!-- 聊天消息 -->
      <div v-for="(message, index) in messages" :key="index" 
           class="message-container" 
           :class="{ 'user-container': message.role === 'user', 'ai-container': message.role === 'assistant' }">
        <div class="message-header">
          <div class="message-avatar" :class="{ 'user-avatar': message.role === 'user' }">
            {{ message.role === 'user' ? username.charAt(0).toUpperCase() : 'AI' }}
          </div>
          <div class="message-name">{{ message.role === 'user' ? username : 'WHU-AI助手' }}</div>
          <div class="message-time">{{ formatTime(message.timestamp) }}</div>
        </div>
        <div class="message-body" v-html="formatMessage(message.content)"></div>
      </div>
      
      <!-- 加载动画 -->
      <div v-if="loading" class="typing-container">
        <div class="typing-avatar">AI</div>
        <div class="typing-content">
          <div class="typing-name">WHU-AI助手</div>
          <div class="typing-indicator">
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>
      </div>
    </div>
    
    <div class="chat-input">
      <textarea 
        class="input-field" 
        v-model="messageText" 
        placeholder="输入你的问题..." 
        :disabled="loading"
        @keydown.enter.exact.prevent="sendMessage"
        @keydown.ctrl.enter="addNewLine"
        ref="inputField"></textarea>
      <div class="input-actions">
        <button class="send-button" @click="sendMessage" :disabled="loading || !messageText.trim()">
          <i class="send-icon"></i>
          发送
        </button>
      </div>
    </div>
    
    <!-- 清空对话确认弹窗 -->
    <div class="modal-overlay" v-if="showClearConfirm" @click="showClearConfirm = false">
      <div class="modal-content" @click.stop>
        <div class="modal-header">
          <h3>确认清空对话</h3>
        </div>
        <div class="modal-body">
          <p>确定要清空所有聊天记录吗？此操作不可恢复。</p>
        </div>
        <div class="modal-footer">
          <button class="modal-cancel" @click="showClearConfirm = false">取消</button>
          <button class="modal-confirm" @click="clearHistory">确认清空</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, nextTick, watch } from 'vue';
import axios from 'axios';
import { useRoute } from 'vue-router';

export default {
  name: 'AIChat',
  props: {
    userId: {
      type: Number,
      required: true
    },
    username: {
      type: String,
      required: true
    }
  },
  setup(props) {
    const messages = ref([]);
    const messageText = ref('');
    const loading = ref(false);
    const chatContent = ref(null);
    const inputField = ref(null);
    const showClearConfirm = ref(false);
    const route = useRoute();
    
    // 获取用户信息，优先使用props，如果没有则从route.query或localStorage获取
    const userId = ref(props.userId || Number(route.query.userId) || Number(localStorage.getItem('userId')));
    const username = ref(props.username || route.query.username || localStorage.getItem('username'));
    
    if (!userId.value || !username.value) {
      console.error('AI聊天缺少必要的用户信息');
    }
    
    // 预设问题建议
    const suggestions = ref([
      "武汉大学有哪些著名的景点？",
      "请介绍一下武汉大学的历史",
      "武汉大学的计算机专业如何？",
      "武汉大学的图书馆有什么特色？",
      "武汉大学有哪些传统活动？",
      "武汉大学的留学生项目有哪些？"
    ]);
    
    // 加载历史消息
    const loadMessages = async () => {
      try {
        const response = await axios.get(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/history/${userId.value}`);
        if (response.data && response.data.success) {
          const history = response.data.history;
          if (history && history.length > 0) {
            messages.value = history.map(msg => ({
              ...msg,
              timestamp: new Date(msg.timestamp)
            }));
            await nextTick();
            scrollToBottom();
          }
        }
      } catch (error) {
        console.error('加载历史消息失败:', error);
      }
    };
    
    // 使用建议问题
    const useQuestion = (question) => {
      messageText.value = question;
      if (inputField.value) {
        inputField.value.focus();
      }
    };
    
    // 发送消息
    const sendMessage = async () => {
      if (loading.value || !messageText.value.trim()) {
        return;
      }
      
      const userMessage = {
        role: 'user',
        content: messageText.value,
        timestamp: new Date()
      };
      
      // 添加用户消息
      messages.value.push(userMessage);
      
      // 清空输入框
      const userMessageText = messageText.value;
      messageText.value = '';
      
      // 滚动到底部
      await nextTick();
      scrollToBottom();
      
      // 设置加载状态
      loading.value = true;
      
      try {
        // 创建历史消息数组（只传递最近10条消息，避免过长）
        const recentMessages = messages.value.slice(-10).map(msg => ({
          role: msg.role,
          content: msg.content
        }));
        
        // 发送请求
        const response = await axios.post(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/chat`, {
          userId: userId.value,
          username: username.value,
          message: userMessageText,
          history: recentMessages
        });
        
        // 添加AI回复
        if (response.data && response.data.success) {
          messages.value.push({
            role: 'assistant',
            content: response.data.message,
            timestamp: new Date()
          });
        } else {
          // 处理错误
          messages.value.push({
            role: 'assistant',
            content: `抱歉，我遇到了一些问题：${response.data?.error || '未知错误'}`,
            timestamp: new Date()
          });
        }
      } catch (error) {
        console.error('AI请求失败:', error);
        // 添加错误消息
        messages.value.push({
          role: 'assistant',
          content: '抱歉，我暂时无法回应你的问题。请稍后再试。',
          timestamp: new Date()
        });
      } finally {
        // 取消加载状态
        loading.value = false;
        
        // 滚动到底部
        await nextTick();
        scrollToBottom();
      }
    };
    
    // Ctrl+Enter添加换行
    const addNewLine = () => {
      messageText.value += '\n';
    };
    
    // 确认清空历史
    const confirmClearHistory = () => {
      showClearConfirm.value = true;
    };
    
    // 清空聊天历史
    const clearHistory = async () => {
      try {
        const response = await axios.delete(`${window.apiBaseUrl || 'http://localhost:5067'}/api/ai/history/${userId.value}`);
        if (response.data && response.data.success) {
          messages.value = [];
          showClearConfirm.value = false;
        } else {
          console.error('清空历史失败:', response.data?.error);
        }
      } catch (error) {
        console.error('清空历史请求失败:', error);
      }
    };
    
    // 格式化消息（处理换行符和特殊标记）
    const formatMessage = (text) => {
      if (!text) return '';
      
      // 将换行符转换为<br>
      let formatted = text.replace(/\n/g, '<br>');
      
      // 将*text*转换为<strong>text</strong>
      formatted = formatted.replace(/\*([^*]+)\*/g, '<strong>$1</strong>');
      
      // 将_text_转换为<em>text</em>
      formatted = formatted.replace(/\_([^_]+)\_/g, '<em>$1</em>');
      
      return formatted;
    };
    
    // 格式化时间
    const formatTime = (timestamp) => {
      if (!timestamp) return '';
      
      const date = new Date(timestamp);
      return date.toLocaleTimeString('zh-CN', {
        hour: '2-digit',
        minute: '2-digit'
      });
    };
    
    // 滚动到底部
    const scrollToBottom = () => {
      if (chatContent.value) {
        chatContent.value.scrollTop = chatContent.value.scrollHeight;
      }
    };
    
    // 监听消息变化，滚动到底部
    watch(messages, () => {
      nextTick(() => scrollToBottom());
    });
    
    // 组件挂载时
    onMounted(() => {
      // 加载历史消息
      loadMessages();
      
      // 自动聚焦输入框
      if (inputField.value) {
        inputField.value.focus();
      }
    });
    
    return {
      messages,
      messageText,
      loading,
      chatContent,
      inputField,
      showClearConfirm,
      suggestions,
      useQuestion,
      sendMessage,
      addNewLine,
      confirmClearHistory,
      clearHistory,
      formatMessage,
      formatTime,
      scrollToBottom,
      username: username.value
    };
  }
};
</script>

<style scoped>
.ai-chat-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background-color: #f9fafb;
}

.chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  background-color: white;
  border-bottom: 1px solid #eaeaea;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.header-left, .header-right {
  flex: 1;
}

.header-title {
  display: flex;
  align-items: center;
  justify-content: center;
}

.ai-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 12px;
}

.header-title h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #333;
}

.back-button {
  display: flex;
  align-items: center;
  color: #6366f1;
  text-decoration: none;
  font-size: 14px;
}

.back-icon {
  width: 16px;
  height: 16px;
  position: relative;
  margin-right: 6px;
}

.back-icon::before {
  content: "";
  position: absolute;
  width: 10px;
  height: 2px;
  background-color: #6366f1;
  top: 7px;
  left: 0;
  transform: rotate(-45deg);
}

.back-icon::after {
  content: "";
  position: absolute;
  width: 10px;
  height: 2px;
  background-color: #6366f1;
  top: 7px;
  left: 0;
  transform: rotate(45deg);
}

.clear-button {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  background: transparent;
  border: none;
  color: #f43f5e;
  cursor: pointer;
  font-size: 14px;
  padding: 0;
}

.clear-icon {
  width: 16px;
  height: 16px;
  position: relative;
  margin-right: 6px;
}

.clear-icon::before, .clear-icon::after {
  content: "";
  position: absolute;
  width: 16px;
  height: 2px;
  background-color: #f43f5e;
  top: 7px;
  left: 0;
}

.clear-icon::before {
  transform: rotate(45deg);
}

.clear-icon::after {
  transform: rotate(-45deg);
}

.chat-content {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.welcome-container {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  padding: 20px;
  margin-bottom: 20px;
  animation: fadeIn 0.5s ease;
}

.welcome-header {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
}

.welcome-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 15px;
  font-size: 18px;
}

.welcome-header h2 {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  color: #333;
}

.welcome-message {
  margin-bottom: 20px;
  color: #555;
  font-size: 15px;
  line-height: 1.6;
}

.suggestions {
  background-color: #f7f9ff;
  border-radius: 8px;
  padding: 15px;
}

.suggestion-title {
  font-size: 15px;
  font-weight: 600;
  color: #444;
  margin-bottom: 12px;
}

.suggestion-items {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  grid-gap: 10px;
}

.suggestion-item {
  background-color: white;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  padding: 10px 15px;
  font-size: 14px;
  color: #4b5563;
  cursor: pointer;
  transition: all 0.2s ease;
}

.suggestion-item:hover {
  background-color: #eef2ff;
  border-color: #c7d2fe;
  color: #4f46e5;
}

.message-container {
  background-color: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  padding: 15px;
  margin-bottom: 20px;
  max-width: 90%;
  animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.user-container {
  background-color: #ebf5ff;
  margin-left: auto;
}

.ai-container {
  background-color: white;
  margin-right: auto;
}

.message-header {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
}

.message-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 10px;
  flex-shrink: 0;
}

.user-avatar {
  background: linear-gradient(135deg, #1677ff, #69c0ff);
}

.message-name {
  font-size: 14px;
  font-weight: 600;
  color: #333;
  margin-right: 10px;
}

.message-time {
  font-size: 12px;
  color: #999;
}

.message-body {
  font-size: 15px;
  line-height: 1.6;
  color: #333;
  white-space: pre-line;
}

.typing-container {
  display: flex;
  align-items: flex-start;
  margin-bottom: 20px;
  max-width: 90%;
}

.typing-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  margin-right: 10px;
  flex-shrink: 0;
}

.typing-content {
  background-color: white;
  border-radius: 12px;
  padding: 15px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.typing-name {
  font-size: 14px;
  font-weight: 600;
  color: #333;
  margin-bottom: 10px;
}

.typing-indicator {
  display: flex;
}

.typing-indicator span {
  width: 10px;
  height: 10px;
  margin: 0 2px;
  background-color: #6366f1;
  border-radius: 50%;
  display: inline-block;
  animation: blink 1.4s infinite both;
}

.typing-indicator span:nth-child(2) {
  animation-delay: 0.2s;
}

.typing-indicator span:nth-child(3) {
  animation-delay: 0.4s;
}

@keyframes blink {
  0% {
    opacity: 0.2;
  }
  20% {
    opacity: 1;
  }
  100% {
    opacity: 0.2;
  }
}

.chat-input {
  padding: 15px 20px;
  background-color: white;
  border-top: 1px solid #eaeaea;
  display: flex;
  flex-direction: column;
}

.input-field {
  width: 100%;
  min-height: 60px;
  max-height: 150px;
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 12px 15px;
  font-size: 15px;
  resize: none;
  outline: none;
  transition: border-color 0.3s ease;
  margin-bottom: 10px;
}

.input-field:focus {
  border-color: #6366f1;
}

.input-actions {
  display: flex;
  justify-content: flex-end;
}

.send-button {
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #6366f1;
  color: white;
  border: none;
  border-radius: 6px;
  padding: 8px 16px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.3s ease;
}

.send-button:hover {
  background-color: #4f46e5;
}

.send-button:disabled {
  background-color: #d1d5db;
  cursor: not-allowed;
}

.send-icon {
  width: 16px;
  height: 16px;
  margin-right: 6px;
  position: relative;
}

.send-icon::before {
  content: "";
  position: absolute;
  width: 14px;
  height: 2px;
  background-color: white;
  top: 7px;
  left: 0;
  transform: rotate(45deg);
}

.send-icon::after {
  content: "";
  position: absolute;
  width: 8px;
  height: 2px;
  background-color: white;
  top: 11px;
  left: 2px;
  transform: rotate(-45deg);
}

/* 模态框样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background-color: white;
  border-radius: 8px;
  width: 400px;
  max-width: 90%;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  overflow: hidden;
}

.modal-header {
  padding: 15px 20px;
  border-bottom: 1px solid #eaeaea;
}

.modal-header h3 {
  margin: 0;
  font-size: 16px;
  color: #333;
}

.modal-body {
  padding: 20px;
}

.modal-body p {
  margin: 0;
  font-size: 14px;
  color: #666;
  line-height: 1.5;
}

.modal-footer {
  padding: 15px 20px;
  border-top: 1px solid #eaeaea;
  display: flex;
  justify-content: flex-end;
}

.modal-cancel, .modal-confirm {
  padding: 8px 16px;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  margin-left: 10px;
}

.modal-cancel {
  background-color: #f9fafb;
  border: 1px solid #d1d5db;
  color: #6b7280;
}

.modal-confirm {
  background-color: #f43f5e;
  border: none;
  color: white;
}

/* 响应式设计 */
@media (max-width: 768px) {
  .suggestion-items {
    grid-template-columns: 1fr;
  }
  
  .message-container {
    max-width: 95%;
  }
}
</style> 