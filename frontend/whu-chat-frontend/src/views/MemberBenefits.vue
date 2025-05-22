<template>
  <div class="member-benefits-container">
    <!-- 返回按钮 -->
    <div class="back-to-home">
      <button class="back-btn" @click="goToHome">
        <i class="back-icon"></i>
        返回主页
      </button>
    </div>
    
    <!-- 顶部标题区域 -->
    <div class="header-section">
      <h1 class="main-title">会员权益中心</h1>
      <p class="subtitle">解锁更多精彩功能，尽享校园社交新体验</p>
    </div>
    
    <!-- 用户当前会员状态 -->
    <div class="current-status">
      <div class="user-level" :class="levelClass">
        <img :src="levelIcon" alt="会员等级" class="level-icon">
        <div class="level-info">
          <h2>{{ levelText }}</h2>
          <p v-if="userLevel > 0">有效期至: {{ formatDate(memberExpireDate) }}</p>
          <p v-else>开通会员，享受更多权益</p>
        </div>
      </div>
    </div>
    
    <!-- 会员卡片区域 -->
    <div class="membership-cards">
      <!-- 普通会员卡片 -->
      <div class="card regular" :class="{ 'active': userLevel === 0 }">
        <div class="card-header">
          <h3>普通用户</h3>
          <span class="price">免费</span>
        </div>
        <div class="card-content">
          <ul class="benefits-list">
            <li><i class="feature-icon check"></i>基础聊天功能</li>
            <li><i class="feature-icon check"></i>参与公共聊天室</li>
            <li><i class="feature-icon check"></i>好友添加(最多20人)</li>
            <li><i class="feature-icon check"></i>创建群聊(最多1个)</li>
            <li><i class="feature-icon times"></i>个性化主题</li>
            <li><i class="feature-icon times"></i>高级AI助手</li>
            <li><i class="feature-icon times"></i>消息云存储</li>
            <li><i class="feature-icon times"></i>大文件传输</li>
          </ul>
        </div>
        <div class="current-plan" v-if="userLevel === 0">当前方案</div>
      </div>
      
      <!-- VIP会员卡片 -->
      <div class="card vip" :class="{ 'active': userLevel === 1 }">
        <div class="card-badge">高性价比</div>
        <div class="card-header">
          <h3>VIP会员</h3>
          <span class="price">¥9.9<small>/月</small></span>
        </div>
        <div class="card-content">
          <ul class="benefits-list">
            <li><i class="feature-icon check"></i>基础聊天功能</li>
            <li><i class="feature-icon check"></i>参与公共聊天室</li>
            <li><i class="feature-icon check"></i>好友添加(最多100人)</li>
            <li><i class="feature-icon check"></i>创建群聊(最多5个)</li>
            <li><i class="feature-icon check"></i>5套个性化主题</li>
            <li><i class="feature-icon check"></i>基础AI助手</li>
            <li><i class="feature-icon check"></i>7天消息云存储</li>
            <li><i class="feature-icon check"></i>100MB文件传输</li>
          </ul>
        </div>
        <button class="subscribe-btn" @click="selectPlan(1)" :disabled="userLevel === 1">
          {{ userLevel === 1 ? '当前方案' : '立即开通' }}
        </button>
      </div>
      
      <!-- SVIP会员卡片 -->
      <div class="card svip" :class="{ 'active': userLevel === 2 }">
        <div class="card-badge">尊享特权</div>
        <div class="card-header">
          <h3>SVIP会员</h3>
          <span class="price">¥19.9<small>/月</small></span>
        </div>
        <div class="card-content">
          <ul class="benefits-list">
            <li><i class="feature-icon check"></i>全部基础功能</li>
            <li><i class="feature-icon check"></i>专属聊天室</li>
            <li><i class="feature-icon check"></i>好友添加(无限制)</li>
            <li><i class="feature-icon check"></i>创建群聊(无限制)</li>
            <li><i class="feature-icon check"></i>无限个性化主题</li>
            <li><i class="feature-icon check"></i>高级AI助手</li>
            <li><i class="feature-icon check"></i>永久消息云存储</li>
            <li><i class="feature-icon check"></i>1GB文件传输</li>
          </ul>
        </div>
        <button class="subscribe-btn svip-btn" @click="selectPlan(2)" :disabled="userLevel === 2">
          {{ userLevel === 2 ? '当前方案' : '立即开通' }}
        </button>
      </div>
    </div>
    
    <!-- 会员特权详情 -->
    <div class="benefits-details">
      <h2>会员特权详解</h2>
      
      <div class="benefits-grid">
        <div class="benefit-item">
          <div class="benefit-icon theme-icon"></div>
          <h3>个性化主题</h3>
          <p>专属主题皮肤，让你的聊天界面与众不同</p>
        </div>
        
        <div class="benefit-item">
          <div class="benefit-icon ai-icon"></div>
          <h3>高级AI助手</h3>
          <p>更智能的AI助手，随时解答学习与生活问题</p>
        </div>
        
        <div class="benefit-item">
          <div class="benefit-icon storage-icon"></div>
          <h3>消息云存储</h3>
          <p>聊天记录云端备份，换设备也不怕丢失</p>
        </div>
        
        <div class="benefit-item">
          <div class="benefit-icon file-icon"></div>
          <h3>大文件传输</h3>
          <p>支持更大的文件传输，共享学习资料更方便</p>
        </div>
      </div>
    </div>
    
    <!-- 支付弹窗 -->
    <div class="payment-modal" v-if="showPaymentModal">
      <div class="modal-content">
        <div class="modal-header">
          <h2>订阅确认</h2>
          <button class="close-btn" @click="showPaymentModal = false">×</button>
        </div>
        
        <div class="modal-body">
          <h3>您即将订阅 {{ selectedPlan === 1 ? 'VIP会员' : 'SVIP会员' }}</h3>
          <p class="price-info">价格: ¥{{ selectedPlan === 1 ? '9.9' : '19.9' }}/月</p>
          
          <div class="payment-options">
            <h4>请选择支付方式</h4>
            <div class="payment-methods">
              <label class="payment-method" :class="{ 'selected': paymentMethod === 'wechat' }">
                <input type="radio" name="payment" value="wechat" v-model="paymentMethod">
                <div class="method-icon wechat-icon"></div>
                <span>微信支付</span>
              </label>
              
              <label class="payment-method" :class="{ 'selected': paymentMethod === 'alipay' }">
                <input type="radio" name="payment" value="alipay" v-model="paymentMethod">
                <div class="method-icon alipay-icon"></div>
                <span>支付宝</span>
              </label>
            </div>
          </div>
          
          <button class="confirm-btn" @click="processPurchase">确认支付</button>
          <p class="terms">点击"确认支付"，表示您同意<a href="#" @click.prevent="showTerms = true">《会员服务条款》</a></p>
        </div>
      </div>
    </div>
    
    <!-- 支付扫码弹窗 -->
    <div class="payment-qrcode-modal" v-if="showQRCodeModal">
      <div class="modal-content">
        <div class="modal-header">
          <h2>{{ paymentMethod === 'wechat' ? '微信支付' : '支付宝' }}扫码支付</h2>
          <button class="close-btn" @click="cancelPayment">×</button>
        </div>
        
        <div class="modal-body">
          <div class="payment-info">
            <div class="payment-amount">¥{{ selectedPlan === 1 ? '9.9' : '19.9' }}</div>
            <div class="payment-title">{{ selectedPlan === 1 ? 'VIP会员' : 'SVIP会员' }}月度订阅</div>
          </div>
          
          <div class="qrcode-container">
            <img src="https://eaxon-bucket.oss-cn-wuhan-lr.aliyuncs.com/mock.png" alt="支付二维码" class="qrcode-image">
            <div class="qrcode-mask" v-if="paymentStatus === 'processing'">
              <div class="qrcode-status">
                <div class="spinner"></div>
                <p>支付处理中...</p>
              </div>
            </div>
            <div class="qrcode-mask success" v-if="paymentStatus === 'success'">
              <div class="qrcode-status">
                <div class="success-icon">✓</div>
                <p>支付成功!</p>
              </div>
            </div>
          </div>
          
          <div class="qrcode-tips">
            <p>请使用{{ paymentMethod === 'wechat' ? '微信' : '支付宝' }}扫描二维码完成支付</p>
            <p class="countdown" v-if="paymentCountdown > 0">二维码有效期: {{ Math.floor(paymentCountdown / 60) }}:{{ (paymentCountdown % 60).toString().padStart(2, '0') }}</p>
          </div>
        </div>
      </div>
    </div>
    
    <!-- 服务条款弹窗 -->
    <div class="terms-modal" v-if="showTerms">
      <div class="modal-content">
        <div class="modal-header">
          <h2>会员服务条款</h2>
          <button class="close-btn" @click="showTerms = false">×</button>
        </div>
        
        <div class="modal-body terms-content">
          <h3>武汉大学学生互助交流平台会员服务条款</h3>
          <p>最后更新日期：2025年5月16日</p>
          
          <div class="terms-section">
            <h4>1. 服务说明</h4>
            <p>本服务条款适用于武汉大学学生互助交流平台（下称"平台"）所提供的会员服务。通过开通会员服务，您将获得特定的功能和权益，包括但不限于个性化主题、高级AI助手、消息云存储和大文件传输等。</p>
          </div>
          
          <div class="terms-section">
            <h4>2. 订阅与续费</h4>
            <p>会员服务采用订阅制，有效期自支付成功之日起计算。除非您主动取消，否则系统将在订阅到期前自动续费。您可以随时在会员中心取消自动续费。</p>
          </div>
          
          <div class="terms-section">
            <h4>3. 退款政策</h4>
            <p>会员服务开通后不支持退款，请谨慎选择会员类型和支付方式。</p>
          </div>
          
          <button class="confirm-btn" @click="showTerms = false">我已阅读并同意</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import axios from 'axios';
import { useStore } from 'vuex/dist/vuex.esm-bundler.js';
import { useRouter } from 'vue-router';

export default {
  name: 'MemberBenefits',
  setup() {
    const store = useStore();
    const router = useRouter();
    
    // 用户状态 - 增加安全检查
    const userInfo = computed(() => {
      try {
        // 首先尝试从Vuex获取
        if (store && store.getters && store.getters['user/userInfo']) {
          return store.getters['user/userInfo'];
        }
        
        // 如果Vuex不可用，尝试从本地存储获取
        const userDataStr = localStorage.getItem('userInfo');
        if (userDataStr) {
          try {
            return JSON.parse(userDataStr);
          } catch (e) {
            console.error('解析本地存储的用户数据失败', e);
          }
        }
        
        // 如果都失败了，从单独的localStorage项中重建
        return {
          id: localStorage.getItem('userId'),
          username: localStorage.getItem('username'),
          avatar: localStorage.getItem('userAvatar'),
          email: localStorage.getItem('userEmail'),
          phone: localStorage.getItem('userPhone'),
          level: 0
        };
      } catch (error) {
        console.error('获取用户信息失败', error);
        return {
          id: localStorage.getItem('userId'),
          username: localStorage.getItem('username') || '访客',
          level: 0
        };
      }
    });
    
    // 初始化用户会员级别和到期日期
    const userLevel = ref(0);
    const memberExpireDate = ref(null);
    
    // 支付相关状态
    const showPaymentModal = ref(false);
    const showQRCodeModal = ref(false);
    const showTerms = ref(false);
    const selectedPlan = ref(0);
    const paymentMethod = ref('wechat');
    const paymentStatus = ref('waiting'); // waiting, processing, success, failed
    const paymentCountdown = ref(300); // 5分钟二维码有效期
    
    // 计时器
    let countdownTimer = null;
    let paymentCheckTimer = null;
    
    // 计算会员等级相关信息
    const levelText = computed(() => {
      switch(userLevel.value) {
        case 1: return 'VIP会员';
        case 2: return 'SVIP会员';
        default: return '普通用户';
      }
    });
    
    const levelClass = computed(() => {
      switch(userLevel.value) {
        case 1: return 'vip-level';
        case 2: return 'svip-level';
        default: return 'regular-level';
      }
    });
    
    const levelIcon = computed(() => {
      switch(userLevel.value) {
        case 1: return `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="60" height="60"><path fill="%23ff9800" d="M12,2L15.09,8.26L22,9.27L17,14.14L18.18,21.02L12,17.77L5.82,21.02L7,14.14L2,9.27L8.91,8.26L12,2M12,5.5L10.16,9.32L6.3,9.92L9.15,12.7L8.47,16.54L12,14.72L15.53,16.54L14.85,12.7L17.7,9.92L13.84,9.32L12,5.5Z"/></svg>`;
        case 2: return `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="60" height="60"><path fill="%23e91e63" d="M12,2L15.09,8.26L22,9.27L17,14.14L18.18,21.02L12,17.77L5.82,21.02L7,14.14L2,9.27L8.91,8.26L12,2M12,5.5L10.16,9.32L6.3,9.92L9.15,12.7L8.47,16.54L12,14.72L15.53,16.54L14.85,12.7L17.7,9.92L13.84,9.32L12,5.5Z"/></svg>`;
        default: return `data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="60" height="60"><circle cx="12" cy="12" r="10" fill="%23e0e0e0" stroke="%23bdbdbd" stroke-width="1"/><path fill="%23757575" d="M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4M12,6A6,6 0 0,1 18,12A6,6 0 0,1 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6M12,8A4,4 0 0,0 8,12A4,4 0 0,0 12,16A4,4 0 0,0 16,12A4,4 0 0,0 12,8Z"/></svg>`;
      }
    });
    
    // 获取用户会员信息
    const fetchMemberInfo = async () => {
      try {
        // 从API获取用户会员信息
        const token = localStorage.getItem('token');
        if (!token) {
          console.warn('未找到登录令牌，尝试从本地获取用户信息');
          // 尝试从Vuex或本地存储获取用户信息
          userLevel.value = userInfo.value.level || 0;
          
          // 如果有会员，设置一个临时的过期时间
          if (userLevel.value > 0) {
            const today = new Date();
            const expireDate = new Date();
            expireDate.setDate(today.getDate() + 30);
            memberExpireDate.value = expireDate;
          }
          return;
        }
        
        try {
          // 调用会员API获取订阅信息
          const response = await axios.get('/api/membership/current', {
            headers: { Authorization: `Bearer ${token}` }
          });
          
          if (response.data) {
            // 如果有活跃订阅
            userLevel.value = response.data.level;
            memberExpireDate.value = new Date(response.data.endDate);
            
            // 更新Vuex存储
            if (store && store.dispatch) {
              store.dispatch('user/updateUserLevel', response.data.level);
            } else {
              // 如果Vuex不可用，更新本地存储
              updateLocalStorageUserLevel(response.data.level);
            }
          } else {
            // 从用户信息获取level
            userLevel.value = userInfo.value.level || 0;
            
            if (userLevel.value > 0) {
              // 假设会员有效期为当前日期后的30天
              const today = new Date();
              const expireDate = new Date();
              expireDate.setDate(today.getDate() + 30);
              memberExpireDate.value = expireDate;
            }
          }
        } catch (apiError) {
          console.error('API请求失败', apiError);
          throw apiError;
        }
      } catch (error) {
        console.error('获取会员信息失败', error);
        // 从本地用户信息获取level作为备用
        userLevel.value = userInfo.value.level || 0;
        
        // 添加调试信息帮助排查问题
        console.log('当前用户信息状态:', {
          userInfoExists: !!userInfo.value,
          userInfoKeys: Object.keys(userInfo.value || {}),
          userLevel: userLevel.value,
          storeExists: !!store,
          storeGettersExists: store && !!store.getters,
          userGettersExists: store && store.getters && !!store.getters['user/userInfo']
        });
      }
    };
    
    // 更新本地存储的用户级别
    const updateLocalStorageUserLevel = (level) => {
      try {
        const userDataStr = localStorage.getItem('userInfo');
        if (userDataStr) {
          const userData = JSON.parse(userDataStr);
          userData.level = level;
          localStorage.setItem('userInfo', JSON.stringify(userData));
        }
      } catch (error) {
        console.error('更新本地存储中的用户级别失败', error);
      }
    };
    
    // 格式化日期
    const formatDate = (date) => {
      if (!date) return '';
      const d = new Date(date);
      return `${d.getFullYear()}-${(d.getMonth() + 1).toString().padStart(2, '0')}-${d.getDate().toString().padStart(2, '0')}`;
    };
    
    // 选择会员方案
    const selectPlan = (plan) => {
      selectedPlan.value = plan;
      showPaymentModal.value = true;
    };
    
    // 显示支付二维码
    const showQRCode = () => {
      showPaymentModal.value = false;
      showQRCodeModal.value = true;
      paymentStatus.value = 'waiting';
      paymentCountdown.value = 300; // 重置为5分钟
      
      // 开始倒计时
      startCountdown();
      
      // 5秒后自动进入支付中状态
      setTimeout(() => {
        if (paymentStatus.value === 'waiting') {
          simulatePayment();
        }
      }, 5000);
    };
    
    // 开始倒计时
    const startCountdown = () => {
      // 清除之前的定时器
      if (countdownTimer) clearInterval(countdownTimer);
      
      countdownTimer = setInterval(() => {
        if (paymentCountdown.value > 0) {
          paymentCountdown.value--;
        } else {
          // 二维码过期
          clearInterval(countdownTimer);
          if (paymentStatus.value === 'waiting' || paymentStatus.value === 'processing') {
            paymentStatus.value = 'failed';
            alert('支付超时，请重新操作');
            showQRCodeModal.value = false;
          }
        }
      }, 1000);
    };
    
    // 模拟支付处理 
    const simulatePayment = async () => {
      paymentStatus.value = 'processing';
      
      // 模拟支付处理
      setTimeout(async () => {
        try {
          // 创建订阅对象
          const subscription = {
            userId: parseInt(userInfo.value.id) || parseInt(localStorage.getItem('userId')),
            level: selectedPlan.value,
            paymentMethod: paymentMethod.value
          };
          
          // 调用后端API创建订阅
          const token = localStorage.getItem('token');
          if (!token) {
            throw new Error('用户未登录');
          }
          
          // 使用mock-payment端点，替换原来的subscribe
          const response = await axios.post('/api/membership/mock-payment', subscription, {
            headers: { Authorization: `Bearer ${token}` }
          });
          
          if (response.data && response.data.success) {
            // 支付成功
            paymentStatus.value = 'success';
            
            // 更新用户会员级别
            userLevel.value = selectedPlan.value;
            
            // 设置会员过期时间
            const today = new Date();
            const expireDate = new Date();
            expireDate.setDate(today.getDate() + 30); // 30天后
            memberExpireDate.value = expireDate;
            
            // 更新Vuex状态
            if (store && store.dispatch) {
              store.dispatch('user/updateUserLevel', selectedPlan.value);
            } else {
              // 如果Vuex不可用，更新本地存储
              updateLocalStorageUserLevel(selectedPlan.value);
            }
            
            // 3秒后关闭支付窗口
            setTimeout(() => {
              showQRCodeModal.value = false;
            }, 3000);
          } else {
            throw new Error(response.data.message || '支付处理失败');
          }
        } catch (error) {
          console.error('处理支付失败', error);
          paymentStatus.value = 'failed';
          alert('支付处理失败，请稍后重试');
          showQRCodeModal.value = false;
        }
      }, 5000); // 模拟5秒后支付完成
    };
    
    // 取消支付
    const cancelPayment = () => {
      // 清除定时器
      if (countdownTimer) clearInterval(countdownTimer);
      
      showQRCodeModal.value = false;
      paymentStatus.value = 'waiting';
    };
    
    // 处理购买流程
    const processPurchase = () => {
      // 显示二维码支付页面
      showQRCode();
    };
    
    // 返回主页
    const goToHome = () => {
      router.push('/home');
    };
    
    // 组件销毁时清除定时器
    onUnmounted(() => {
      if (countdownTimer) clearInterval(countdownTimer);
      if (paymentCheckTimer) clearInterval(paymentCheckTimer);
    });
    
    onMounted(() => {
      // 在组件挂载后获取会员信息
      fetchMemberInfo();
    });
    
    return {
      userLevel,
      memberExpireDate,
      levelText,
      levelClass,
      levelIcon,
      showPaymentModal,
      showQRCodeModal,
      showTerms,
      selectedPlan,
      paymentMethod,
      paymentStatus,
      paymentCountdown,
      formatDate,
      selectPlan,
      processPurchase,
      cancelPayment,
      goToHome
    };
  }
};
</script>

<style scoped>
.member-benefits-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 30px 20px;
  color: #333;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
  position: relative;
}

/* 返回按钮样式 */
.back-to-home {
  position: absolute;
  top: 20px;
  left: 20px;
  z-index: 10;
}

.back-btn {
  display: flex;
  align-items: center;
  background-color: #f5f7fa;
  border: 1px solid #e0e3e9;
  border-radius: 20px;
  padding: 8px 15px;
  cursor: pointer;
  transition: all 0.3s ease;
  color: #333;
  font-size: 14px;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.05);
}

.back-btn:hover {
  background-color: #e0e3e9;
  transform: translateY(-2px);
}

.back-icon {
  width: 16px;
  height: 16px;
  margin-right: 5px;
  position: relative;
  display: inline-block;
}

.back-icon:before {
  content: "";
  position: absolute;
  top: 8px;
  left: 3px;
  width: 10px;
  height: 1px;
  background-color: #333;
}

.back-icon:after {
  content: "";
  position: absolute;
  top: 5px;
  left: 3px;
  width: 6px;
  height: 6px;
  border-left: 1px solid #333;
  border-bottom: 1px solid #333;
  transform: rotate(45deg);
}

/* 顶部标题区域 */
.header-section {
  text-align: center;
  margin-bottom: 40px;
  position: relative;
}

.main-title {
  font-size: 32px;
  font-weight: 600;
  margin-bottom: 10px;
  background: linear-gradient(90deg, #4776E6, #8E54E9);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  animation: shimmer 2s infinite;
}

.subtitle {
  font-size: 16px;
  color: #666;
  max-width: 600px;
  margin: 0 auto;
}

/* 当前会员状态 */
.current-status {
  background: #f7f9fc;
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 40px;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.05);
}

.user-level {
  display: flex;
  align-items: center;
  gap: 20px;
}

.level-icon {
  width: 60px;
  height: 60px;
  object-fit: contain;
}

.level-info h2 {
  font-size: 22px;
  margin: 0 0 5px;
}

.level-info p {
  margin: 0;
  color: #666;
}

/* 会员等级样式 */
.regular-level h2 {
  color: #666;
}

.vip-level h2 {
  color: #ff9800;
  text-shadow: 0 1px 2px rgba(255, 152, 0, 0.2);
}

.svip-level h2 {
  background: linear-gradient(90deg, #FF416C, #FF4B2B);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  text-shadow: 0 1px 2px rgba(255, 75, 43, 0.2);
}

/* 会员卡片 */
.membership-cards {
  display: flex;
  gap: 20px;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 50px;
}

.card {
  flex: 1;
  min-width: 280px;
  max-width: 350px;
  background: #fff;
  border-radius: 12px;
  padding: 25px;
  box-shadow: 0 5px 20px rgba(0, 0, 0, 0.05);
  position: relative;
  transition: all 0.3s ease;
  overflow: hidden;
}

.card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
}

.card.active {
  border: 2px solid;
  transform: scale(1.02);
}

.card.regular {
  border-color: #e0e0e0;
}

.card.vip {
  border-color: #ff9800;
}

.card.svip {
  border-color: #e91e63;
}

.card.regular.active {
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.05);
}

.card.vip.active {
  box-shadow: 0 8px 25px rgba(255, 152, 0, 0.15);
}

.card.svip.active {
  box-shadow: 0 8px 25px rgba(233, 30, 99, 0.15);
}

.card-badge {
  position: absolute;
  top: 10px;
  right: -30px;
  background: #ff9800;
  color: white;
  padding: 5px 30px;
  font-size: 12px;
  transform: rotate(45deg);
  z-index: 1;
}

.card.svip .card-badge {
  background: #e91e63;
}

.card-header {
  padding-bottom: 15px;
  margin-bottom: 20px;
  border-bottom: 1px solid #f0f0f0;
  position: relative;
}

.card-header h3 {
  margin: 0 0 10px;
  font-size: 20px;
}

.card.vip .card-header h3 {
  color: #ff9800;
}

.card.svip .card-header h3 {
  color: #e91e63;
}

.price {
  font-size: 28px;
  font-weight: 700;
  display: block;
}

.price small {
  font-size: 14px;
  opacity: 0.8;
}

.card-content {
  margin-bottom: 25px;
}

.benefits-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.benefits-list li {
  padding: 7px 0;
  display: flex;
  align-items: center;
  gap: 10px;
  color: #555;
}

.feature-icon {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-style: normal;
}

.feature-icon.check::before {
  content: "✓";
  color: #4caf50;
}

.feature-icon.times::before {
  content: "×";
  color: #ff5252;
}

.subscribe-btn {
  width: 100%;
  padding: 12px;
  border: none;
  border-radius: 6px;
  background: #ff9800;
  color: white;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.subscribe-btn:hover {
  background: #f57c00;
  transform: translateY(-2px);
  box-shadow: 0 4px 10px rgba(255, 152, 0, 0.2);
}

.subscribe-btn:disabled {
  background: #9e9e9e;
  cursor: default;
  transform: none;
  box-shadow: none;
}

.svip-btn {
  background: #e91e63;
}

.svip-btn:hover {
  background: #d81b60;
  box-shadow: 0 4px 10px rgba(233, 30, 99, 0.2);
}

.current-plan {
  text-align: center;
  font-weight: bold;
  margin-top: 15px;
  color: #4caf50;
}

/* 会员特权详情 */
.benefits-details {
  padding: 40px 0;
}

.benefits-details h2 {
  text-align: center;
  margin-bottom: 30px;
  font-size: 24px;
}

.benefits-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 25px;
}

.benefit-item {
  background: #fff;
  border-radius: 10px;
  padding: 25px;
  text-align: center;
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.05);
  transition: transform 0.3s;
}

.benefit-item:hover {
  transform: translateY(-5px);
}

.benefit-icon {
  width: 70px;
  height: 70px;
  border-radius: 50%;
  margin: 0 auto 15px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f5f7fa;
}

.theme-icon::before {
  content: "🎨";
  font-size: 30px;
}

.ai-icon::before {
  content: "🤖";
  font-size: 30px;
}

.storage-icon::before {
  content: "☁️";
  font-size: 30px;
}

.file-icon::before {
  content: "📁";
  font-size: 30px;
}

.benefit-item h3 {
  margin: 0 0 10px;
  font-size: 18px;
}

.benefit-item p {
  margin: 0;
  color: #666;
  font-size: 14px;
}

/* 支付弹窗 */
.payment-modal, .terms-modal, .payment-qrcode-modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  animation: fadeIn 0.3s;
}

.modal-content {
  width: 90%;
  max-width: 500px;
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  animation: slideUp 0.3s;
}

.modal-header {
  padding: 15px 20px;
  background: #f5f7fa;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h2 {
  margin: 0;
  font-size: 20px;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  line-height: 1;
  cursor: pointer;
  color: #666;
}

.modal-body {
  padding: 25px;
}

.modal-body h3 {
  margin-top: 0;
  text-align: center;
}

.price-info {
  text-align: center;
  font-size: 20px;
  font-weight: bold;
  color: #e91e63;
  margin-bottom: 25px;
}

.payment-options {
  margin-bottom: 25px;
}

.payment-options h4 {
  margin-bottom: 15px;
}

.payment-methods {
  display: flex;
  gap: 15px;
}

.payment-method {
  flex: 1;
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  padding: 15px;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
}

.payment-method:hover {
  border-color: #bbdefb;
  background: #f5f9ff;
}

.payment-method.selected {
  border-color: #2196f3;
  background: #e3f2fd;
}

.payment-method input {
  display: none;
}

.method-icon {
  width: 40px;
  height: 40px;
  margin: 0 auto 10px;
  background-size: contain;
  background-position: center;
  background-repeat: no-repeat;
}

.wechat-icon {
  background-image: url('data:image/svg+xml;base64,PHN2ZyBjbGFzcz0iaWNvbiIgdmlld0JveD0iMCAwIDEyMjggMTAyNCIgdmVyc2lvbj0iMS4xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHAtaWQ9IjI0MTUiIHdpZHRoPSIyMDAiIGhlaWdodD0iMjAwIj48cGF0aCBkPSJNNTMwLjg5MjggNzAzLjEyOTZhNDEuNDcyIDQxLjQ3MiAwIDAgMS0zNS43Mzc2LTE5LjgxNDRsLTIuNzEzNi01LjU4MDhMMjc4LjI3MiAzOTQuNzUyYTE4LjczOTIgMTguNzM5MiAwIDAgMS0yLjA0OC04LjE0MDhhMTkuOTY4IDE5Ljk2OCAwIDAgMSAyMC40OC0xOS4zNTM2YzQuNjA4IDAgOC44NTc2IDEuNDMzNiAxMi4yODggMy44NEw1NDMuMzk1MiA1MTEuMTc3NmE2NC40MDk2IDY0LjQwOTYgMCAwIDAgNTQuNTI4IDUuOTM5MkwxMTE2LjI2MjQgMjA0LjhDMTAwNC45NTM2IDgwLjg5NiA4MjEuNzYgMCA2MTQuNCAwIDI3NS4wNDY0IDAgMCAyMTYuNTc2IDAgNDgzLjYzNTJjMCAxNDUuNzE1MiA4Mi43MzkyIDI3Ni44ODk2IDIxMi4yNzUyIDM2NS41MTY4YTM4LjE5NTIgMzguMTk1MiAwIDAgMSAxNy4yMDMyIDMxLjQ4OGE0NC40OTI4IDQ0LjQ5MjggMCAwIDEtMi4xNTA0IDEyLjM5MDRsLTI3LjY5OTIgOTcuNDg0OGMtMS4zMzEyIDQuNjA4LTMuMzI4IDkuMzY5Ni0zLjMyOCAxNC4xMzEyIDAgMTAuNzUyIDkuMjE2IDE5LjM1MzYgMjAuNDggMTkuMzUzNiA0LjQwMzIgMCA4LjAzODQtMS41MzYgMTEuNzc2LTMuNTg0bDEzNC41NTM2LTczLjMxODRjMTAuMTM3Ni01LjUyOTYgMjAuNzg3Mi04Ljk2IDMyLjYxNDQtOC45NiA2LjI5NzYgMCAxMi4yODggMC45MjE2IDE4LjA3MzYgMi41MDg4IDYyLjcyIDE3LjA0OTYgMTMwLjQ1NzYgMjYuNTcyOCAyMDAuNTUwNCAyNi41NzI4Qzk1My43MDI0IDk2Ny4xNjggMTIyOC44IDc1MC41OTIgMTIyOC44IDQ4My42MzUyYzAgLTgwLjk0NzItMjUuNDQ2NC0xNTcuMTMyOC03MC4wNDE2LTIyNC4xMDI0bC02MDQuOTc5MiA0MzYuOTkyLTQuNDU0NCAyLjQwNjRhNDIuMTM3NiA0Mi4xMzc2IDAgMCAxLTE4LjQzMiA0LjE5ODR6IiBmaWxsPSIjMTVCQTExIiBwLWlkPSIyNDE2Ij48L3BhdGg+PC9zdmc+');
}

.alipay-icon {
  background-image: url('data:image/svg+xml;utf8,<svg t="1747371207079" class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="3409" width="200" height="200"><path d="M230.771014 576.556522c-12.614493 9.646377-25.228986 23.744928-28.93913 42.295652-5.194203 24.486957-0.742029 55.652174 22.26087 80.13913 28.93913 28.93913 72.718841 37.101449 92.011594 38.585508 51.2 3.710145 106.110145-22.26087 147.663768-50.457971 16.324638-11.130435 43.77971-34.133333 70.492754-69.750725-59.362319-30.423188-133.565217-64.556522-212.22029-61.588406-41.553623 1.484058-70.492754 9.646377-91.269566 20.776812zM983.188406 712.347826c25.971014-61.588406 40.811594-129.113043 40.811594-200.347826 0-281.971014-230.028986-512-512-512S0 230.028986 0 512s230.028986 512 512 512c170.666667 0 321.298551-83.849275 414.794203-212.22029C838.492754 768.742029 693.797101 696.023188 604.011594 652.985507c-42.295652 48.973913-105.368116 97.205797-176.602898 117.982609-44.521739 13.356522-85.333333 18.550725-126.886957 9.646377-42.295652-8.904348-72.718841-28.197101-90.527536-47.489855-8.904348-10.388406-19.292754-22.26087-27.455073-37.843479 0.742029 0.742029 0.742029 2.226087 0.742029 2.968116 0 0-4.452174-7.42029-7.420289-19.292753-1.484058-5.936232-2.968116-11.872464-3.710145-17.808696-0.742029-4.452174-0.742029-8.904348 0-12.614493-0.742029-7.42029 0-15.582609 1.484058-23.744927 4.452174-20.776812 12.614493-43.77971 35.617391-65.298551 48.973913-48.231884 115.014493-50.457971 149.147826-50.457971 50.457971 0.742029 138.017391 22.26087 212.22029 48.973913 20.776812-43.77971 34.133333-89.785507 42.295652-121.692754H304.973913v-33.391304h158.052174v-66.782609H272.324638v-34.133333h190.701449v-66.782609c0-8.904348 2.226087-16.324638 16.324638-16.324637h74.944927v83.107246h207.026087v33.391304H554.295652v66.782609H719.768116S702.701449 494.933333 651.501449 586.202899c115.014493 40.811594 277.518841 104.626087 331.686957 126.144927z m0 0" fill="%2306B4FD" p-id="3410"></path></svg>');
}

.confirm-btn {
  width: 100%;
  padding: 12px;
  background: #2196f3;
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: 15px;
}

.confirm-btn:hover {
  background: #1e88e5;
  box-shadow: 0 4px 10px rgba(33, 150, 243, 0.2);
}

.terms {
  text-align: center;
  font-size: 12px;
  color: #666;
  margin: 0;
}

.terms a {
  color: #2196f3;
  text-decoration: none;
}

/* 服务条款弹窗 */
.terms-content {
  max-height: 60vh;
  overflow-y: auto;
  padding-right: 10px;
}

.terms-section {
  margin-bottom: 20px;
}

.terms-section h4 {
  margin-bottom: 8px;
}

.terms-section p {
  margin: 0;
  color: #666;
  line-height: 1.6;
}

/* 动画 */
@keyframes shimmer {
  0% {
    background-position: -200% 0;
  }
  100% {
    background-position: 200% 0;
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideUp {
  from {
    transform: translateY(30px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* 响应式布局 */
@media (max-width: 768px) {
  .membership-cards {
    flex-direction: column;
    align-items: center;
  }
  
  .card {
    width: 100%;
    max-width: 100%;
  }
  
  .payment-methods {
    flex-direction: column;
  }
}

/* 支付二维码样式 */
.payment-qrcode-modal .modal-content {
  width: 90%;
  max-width: 400px;
}

.payment-info {
  text-align: center;
  margin-bottom: 20px;
}

.payment-amount {
  font-size: 24px;
  font-weight: bold;
  color: #e91e63;
  margin-bottom: 5px;
}

.payment-title {
  font-size: 16px;
  color: #666;
}

.qrcode-container {
  width: 220px;
  height: 220px;
  margin: 0 auto 20px;
  position: relative;
  border: 1px solid #eee;
  padding: 10px;
  background: #fff;
}

.qrcode-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.qrcode-mask {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
}

.qrcode-mask.success {
  background: rgba(76, 175, 80, 0.7);
}

.qrcode-status {
  text-align: center;
  color: white;
}

.spinner {
  width: 40px;
  height: 40px;
  margin: 0 auto 10px;
  border: 4px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 1s ease-in-out infinite;
}

.success-icon {
  width: 40px;
  height: 40px;
  margin: 0 auto 10px;
  background: #4caf50;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  color: white;
}

.qrcode-tips {
  text-align: center;
  color: #666;
  margin-top: 15px;
}

.countdown {
  color: #ff5722;
  font-weight: bold;
  margin-top: 5px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style> 