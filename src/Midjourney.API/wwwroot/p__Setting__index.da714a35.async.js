"use strict";(self.webpackChunkmidjourney_proxy_admin=self.webpackChunkmidjourney_proxy_admin||[]).push([[971],{70069:function(Ze,G,a){a.r(G),a.d(G,{default:function(){return pe}});var X=a(15009),L=a.n(X),Y=a(99289),J=a.n(Y),q=a(5574),j=a.n(q),h=a(67294),_=a(74981),Me=a(90252),Se=a(22777),e=a(85893),ee=function(l){var F=l.value,g=F===void 0?{}:F,t=l.onChange,z=h.useState(JSON.stringify(g,null,2)),y=j()(z,2),Z=y[0],u=y[1],A=h.useState(!0),O=j()(A,2),R=O[0],I=O[1];(0,h.useEffect)(function(){var k=JSON.stringify(g),M=JSON.stringify(JSON.parse(Z));k!==M&&u(JSON.stringify(g,null,2))},[g]);var C=function(M){u(M);try{var N=JSON.parse(M);I(!0),t&&t(N)}catch(E){I(!1)}};return(0,e.jsxs)("div",{style:{width:"100%"},children:[(0,e.jsx)(_.ZP,{mode:"json",theme:"textmate",value:Z,onChange:C,name:"json-editor",editorProps:{$blockScrolling:!0},height:"auto",maxLines:1/0,setOptions:{enableBasicAutocompletion:!0,enableLiveAutocompletion:!0,enableSnippets:!0,showLineNumbers:!0,tabSize:2,useWorker:!1},style:{width:"100%",minHeight:"80px"},fontSize:14,lineHeight:19,showPrintMargin:!0,showGutter:!0,highlightActiveLine:!0}),!R&&(0,e.jsx)("div",{style:{color:"red",marginTop:"8px"},children:"JSON \u683C\u5F0F\u9519\u8BEF\uFF0C\u8BF7\u68C0\u67E5\u8F93\u5165\uFF01"})]})},m=ee,S=a(66927),te=function(){var l=document.createElement("div");l.id="full-screen-loading",l.style.position="fixed",l.style.top="0",l.style.left="0",l.style.width="100%",l.style.height="100%",l.style.zIndex="100",l.style.backgroundColor="#fff",l.innerHTML=`
    <style>
      .loading-title {
        font-size: 1.1rem;
        margin-top: 15px;
      }

      .loading-sub-title {
        margin-top: 20px;
        font-size: 1rem;
        color: #888;
      }

      .page-loading-warp {
        display: contents;
        align-items: center;
        justify-content: center;
        padding: 26px;
      }
      .ant-spin {
        position: absolute;
        display: none;
        -webkit-box-sizing: border-box;
        box-sizing: border-box;
        margin: 0;
        padding: 0;
        color: rgba(0, 0, 0, 0.65);
        color: #1890ff;
        font-size: 14px;
        font-variant: tabular-nums;
        line-height: 1.5;
        text-align: center;
        list-style: none;
        opacity: 0;
        -webkit-transition: -webkit-transform 0.3s
          cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: -webkit-transform 0.3s
          cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86);
        transition: transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86),
          -webkit-transform 0.3s cubic-bezier(0.78, 0.14, 0.15, 0.86);
        -webkit-font-feature-settings: "tnum";
        font-feature-settings: "tnum";
      }

      .ant-spin-spinning {
        position: static;
        display: inline-block;
        opacity: 1;
      }

      .ant-spin-dot {
        position: relative;
        display: inline-block;
        width: 20px;
        height: 20px;
        font-size: 20px;
      }

      .ant-spin-dot-item {
        position: absolute;
        display: block;
        width: 9px;
        height: 9px;
        background-color: #1890ff;
        border-radius: 100%;
        -webkit-transform: scale(0.75);
        -ms-transform: scale(0.75);
        transform: scale(0.75);
        -webkit-transform-origin: 50% 50%;
        -ms-transform-origin: 50% 50%;
        transform-origin: 50% 50%;
        opacity: 0.3;
        -webkit-animation: antspinmove 1s infinite linear alternate;
        animation: antSpinMove 1s infinite linear alternate;
      }

      .ant-spin-dot-item:nth-child(1) {
        top: 0;
        left: 0;
      }

      .ant-spin-dot-item:nth-child(2) {
        top: 0;
        right: 0;
        -webkit-animation-delay: 0.4s;
        animation-delay: 0.4s;
      }

      .ant-spin-dot-item:nth-child(3) {
        right: 0;
        bottom: 0;
        -webkit-animation-delay: 0.8s;
        animation-delay: 0.8s;
      }

      .ant-spin-dot-item:nth-child(4) {
        bottom: 0;
        left: 0;
        -webkit-animation-delay: 1.2s;
        animation-delay: 1.2s;
      }

      .ant-spin-dot-spin {
        -webkit-transform: rotate(45deg);
        -ms-transform: rotate(45deg);
        transform: rotate(45deg);
        -webkit-animation: antrotate 1.2s infinite linear;
        animation: antRotate 1.2s infinite linear;
      }

      .ant-spin-lg .ant-spin-dot {
        width: 32px;
        height: 32px;
        font-size: 32px;
      }

      .ant-spin-lg .ant-spin-dot i {
        width: 14px;
        height: 14px;
      }

      @media all and (-ms-high-contrast: none), (-ms-high-contrast: active) {
        .ant-spin-blur {
          background: #fff;
          opacity: 0.5;
        }
      }

      @-webkit-keyframes antSpinMove {
        to {
          opacity: 1;
        }
      }

      @keyframes antSpinMove {
        to {
          opacity: 1;
        }
      }

      @-webkit-keyframes antRotate {
        to {
          -webkit-transform: rotate(405deg);
          transform: rotate(405deg);
        }
      }

      @keyframes antRotate {
        to {
          -webkit-transform: rotate(405deg);
          transform: rotate(405deg);
        }
      }
      .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100vh;
        min-height: 362px;
      }
    </style>
    <div class="loading-container">
      <div class="page-loading-warp">
        <div class="ant-spin ant-spin-lg ant-spin-spinning">
          <span class="ant-spin-dot ant-spin-dot-spin">
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
            <i class="ant-spin-dot-item"></i>
          </span>
        </div>
        <div class="loading-title">
          \u7CFB\u7EDF\u6B63\u5728\u91CD\u542F\u4E2D...
        </div>
        <div class="loading-sub-title">
          \u7CFB\u7EDF\u6B63\u5728\u91CD\u542F\u4E2D\u53EF\u80FD\u9700\u8981\u8F83\u591A\u65F6\u95F4 \u8BF7\u8010\u5FC3\u7B49\u5F85
        </div>
      </div>
    </div>
  `,document.body.appendChild(l)},ne=function(){var l=document.getElementById("full-screen-loading");l&&l.parentNode&&l.parentNode.removeChild(l)},ae=a(60219),se=a(87740),ie=a(90930),re=a(94272),n=a(53025),c=a(2453),le=a(74330),Q=a(42075),oe=a(40056),ge=a(83062),f=a(55102),D=a(83622),de=a(28248),H=a(71230),B=a(15746),w=a(4393),s=a(72269),i=a(74656),x=a(37804),me=function(){var l=n.Z.useForm(),F=j()(l,1),g=F[0],t=(0,re.useIntl)(),z=(0,h.useState)(!1),y=j()(z,2),Z=y[0],u=y[1],A=function(){u(!0),(0,S.iE)().then(function(r){u(!1),r.success&&g.setFieldsValue(r.data)})};(0,h.useEffect)(function(){A()},[]);var O=function(){g.validateFields().then(function(r){u(!0),(0,S.rF)(r).then(function(b){u(!1),b.success?(c.ZP.success(t.formatMessage({id:"pages.setting.saveSuccess"})),A()):c.ZP.error(b.message||t.formatMessage({id:"pages.setting.error"}))})}).catch(function(){c.ZP.error(t.formatMessage({id:"pages.setting.error"}))})},R=(0,h.useState)(""),I=j()(R,2),C=I[0],k=I[1],M=(0,h.useState)(""),N=j()(M,2),E=N[0],ce=N[1],ue=(0,h.useState)(!1),$=j()(ue,2),fe=$[0],V=$[1],he=(0,h.useState)(""),W=j()(he,2),K=W[0],xe=W[1],je=function(){var p=J()(L()().mark(function r(b){var T,v;return L()().wrap(function(d){for(;;)switch(d.prev=d.next){case 0:return V(!1),te(),T=new Promise(function(o){return setTimeout(o,3e3)}),v=null,d.prev=4,d.next=7,(0,S.df)({password:b});case 7:v=d.sent,v.success?c.ZP.success(v.message):v.code===504&&c.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D..."),d.next=14;break;case 11:d.prev=11,d.t0=d.catch(4),c.ZP.warning("\u7CFB\u7EDF\u6B63\u5728\u52A0\u8F7D\u4E2D...");case 14:return d.prev=14,d.next=17,T;case 17:return ne(),d.finish(14);case 19:case"end":return d.stop()}},r,null,[[4,11,14,19]])}));return function(b){return p.apply(this,arguments)}}(),be=function(){var p=J()(L()().mark(function r(b,T){var v,P;return L()().wrap(function(o){for(;;)switch(o.prev=o.next){case 0:return o.prev=0,u(!0),v={Host:b,ApiSecret:T},o.next=5,(0,S.gU)(v);case 5:P=o.sent,P.success?c.ZP.success(t.formatMessage({id:"pages.setting.migrateSuccess"})):c.ZP.error(P.message),o.next=12;break;case 9:o.prev=9,o.t0=o.catch(0),c.ZP.error(o.t0);case 12:return o.prev=12,u(!1),o.finish(12);case 15:case"end":return o.stop()}},r,null,[[0,9,12,15]])}));return function(b,T){return p.apply(this,arguments)}}(),ve=function(){C?be(C,E):c.ZP.warning(t.formatMessage({id:"pages.setting.migrateTips"}))};return(0,e.jsx)(ie._z,{children:(0,e.jsx)(n.Z,{form:g,labelAlign:"left",layout:"horizontal",labelCol:{span:6},wrapperCol:{span:18},children:(0,e.jsxs)(le.Z,{spinning:Z,children:[(0,e.jsxs)(Q.Z,{style:{marginBottom:"10px",display:"flex",justifyContent:"space-between"},children:[(0,e.jsx)(oe.Z,{type:"info",style:{paddingTop:"4px",paddingBottom:"4px"},description:t.formatMessage({id:"pages.setting.tips"})}),(0,e.jsxs)(Q.Z,{children:[(0,e.jsx)(ge.Z,{placement:"bottom",title:(0,e.jsxs)("div",{style:{display:"flex",flexDirection:"column",gap:8,padding:8},children:[(0,e.jsx)(f.Z,{style:{marginBottom:8},placeholder:"mjplus host",value:C,onChange:function(r){return k(r.target.value)}}),(0,e.jsx)(f.Z,{placeholder:"mj-api-secret",value:E,onChange:function(r){return ce(r.target.value)}})]}),children:(0,e.jsx)(D.ZP,{loading:Z,type:"primary",ghost:!0,onClick:ve,children:t.formatMessage({id:"pages.setting.migrate"})})}),(0,e.jsx)(D.ZP,{loading:Z,icon:(0,e.jsx)(ae.Z,{}),type:"primary",onClick:O,children:t.formatMessage({id:"pages.setting.save"})}),(0,e.jsx)(D.ZP,{loading:Z,icon:(0,e.jsx)(se.Z,{}),type:"primary",onClick:function(){return V(!0)},children:t.formatMessage({id:"pages.setting.restart"})}),(0,e.jsx)(de.Z,{open:fe,onCancel:function(){V(!1),g.resetFields()},onOk:function(){g.validateFields().then(function(){je(K),g.resetFields()}).catch(function(r){console.log("Validation failed:",r)})},okText:"\u786E\u8BA4",cancelText:"\u53D6\u6D88",children:(0,e.jsx)(n.Z,{form:g,layout:"vertical",initialValues:{remember:!0},children:(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.restartModalTip"}),name:"password",rules:[{required:!0,message:t.formatMessage({id:"pages.setting.restartModalTip"})}],children:(0,e.jsx)(f.Z.Password,{placeholder:t.formatMessage({id:"pages.setting.restartModalTip"}),value:K,onChange:function(r){xe(r.target.value),r.target.value.trim()!==""&&g.validateFields(["password"])}})})})})]})]}),(0,e.jsxs)(H.Z,{gutter:16,children:[(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(w.Z,{title:t.formatMessage({id:"pages.setting.accountSetting"}),bordered:!1,children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSwagger"}),name:"enableSwagger",extra:g.getFieldValue("enableSwagger")?(0,e.jsx)("a",{href:"/swagger",target:"_blank",rel:"noreferrer",children:t.formatMessage({id:"pages.setting.swaggerLink"})}):"",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseType"}),name:"databaseType",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"LiteDB",children:"LiteDB"}),(0,e.jsx)(i.Z.Option,{value:"MongoDB",children:"MongoDB"}),(0,e.jsx)(i.Z.Option,{value:"SQLite",children:"SQLite"}),(0,e.jsx)(i.Z.Option,{value:"MySQL",children:"MySQL"}),(0,e.jsx)(i.Z.Option,{value:"PostgreSQL",children:"PostgreSQL"}),(0,e.jsx)(i.Z.Option,{value:"SQLServer",children:"SQLServer"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseConnectionString"}),name:"databaseConnectionString",extra:(0,e.jsx)(e.Fragment,{children:(0,e.jsx)(D.ZP,{style:{marginTop:8},type:"primary",onClick:function(){u(!0),(0,S.yk)().then(function(r){u(!1),r.success?c.ZP.success(t.formatMessage({id:"pages.setting.connectSuccess"})):c.ZP.error(r.message||t.formatMessage({id:"pages.setting.connectError"}))})},children:t.formatMessage({id:"pages.setting.testConnect"})})}),children:(0,e.jsx)(f.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.databaseName"}),name:"databaseName",children:(0,e.jsx)(f.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.isAutoMigrate"}),name:"isAutoMigrate",tooltip:t.formatMessage({id:"pages.setting.isAutoMigrateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.maxCount"}),name:"maxCount",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.accountChooseRule"}),name:"accountChooseRule",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"BestWaitIdle",children:"BestWaitIdle"}),(0,e.jsx)(i.Z.Option,{value:"Random",children:"Random"}),(0,e.jsx)(i.Z.Option,{value:"Weight",children:"Weight"}),(0,e.jsx)(i.Z.Option,{value:"Polling",children:"Polling"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.discordConfig"}),name:"ngDiscord",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.proxyConfig"}),name:"proxy",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.imageStorageType"}),name:"imageStorageType",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"NONE",children:"NULL"}),(0,e.jsx)(i.Z.Option,{value:"LOCAL",children:"LOCAL"}),(0,e.jsx)(i.Z.Option,{value:"OSS",children:"Aliyun OSS"}),(0,e.jsx)(i.Z.Option,{value:"COS",children:"Tencent COS"}),(0,e.jsx)(i.Z.Option,{value:"R2",children:"Cloudflare R2"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.localStorage"}),name:"localStorage",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.aliyunOss"}),name:"aliyunOss",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.tencentCos"}),name:"tencentCos",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.cloudflareR2"}),name:"cloudflareR2",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.replicate"}),name:"replicate",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.translate"}),name:"translateWay",children:(0,e.jsxs)(i.Z,{allowClear:!0,children:[(0,e.jsx)(i.Z.Option,{value:"NULL",children:"NULL"}),(0,e.jsx)(i.Z.Option,{value:"BAIDU",children:"BAIDU"}),(0,e.jsx)(i.Z.Option,{value:"GPT",children:"GPT"})]})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.baiduTranslate"}),name:"baiduTranslate",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.openai"}),name:"openai",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.smtp"}),name:"smtp",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notifyHook"}),name:"notifyHook",children:(0,e.jsx)(f.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notifyPoolSize"}),name:"notifyPoolSize",children:(0,e.jsx)(x.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaServer"}),name:"captchaServer",help:t.formatMessage({id:"pages.setting.captchaServerTip"}),children:(0,e.jsx)(f.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaNotifyHook"}),name:"captchaNotifyHook",help:t.formatMessage({id:"pages.setting.captchaNotifyHookTip"}),children:(0,e.jsx)(f.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.captchaNotifySecret"}),name:"captchaNotifySecret",help:t.formatMessage({id:"pages.setting.captchaNotifySecretTip"}),children:(0,e.jsx)(f.Z,{})})]})}),(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(w.Z,{title:t.formatMessage({id:"pages.setting.otherSetting"}),bordered:!1,children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAccountSponsor"}),name:"enableAccountSponsor",help:t.formatMessage({id:"pages.setting.enableAccountSponsorTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.isVerticalDomain"}),name:"isVerticalDomain",help:t.formatMessage({id:"pages.setting.isVerticalDomainTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableRegister"}),name:"enableRegister",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultDayLimit"}),name:"registerUserDefaultDayLimit",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultTotalLimit"}),name:"registerUserDefaultTotalLimit",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultCoreSize"}),name:"registerUserDefaultCoreSize",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.registerUserDefaultQueueSize"}),name:"registerUserDefaultQueueSize",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableGuest"}),name:"enableGuest",children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultDayLimit"}),name:"guestDefaultDayLimit",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultCoreSize"}),name:"guestDefaultCoreSize",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.guestDefaultQueueSize"}),name:"guestDefaultQueueSize",children:(0,e.jsx)(x.Z,{min:-1})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.bannedLimiting"}),name:"bannedLimiting",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.ipRateLimiting"}),name:"ipRateLimiting",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.ipBlackRateLimiting"}),name:"ipBlackRateLimiting",children:(0,e.jsx)(m,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.notify"}),name:"notify",children:(0,e.jsx)(f.Z.TextArea,{autoSize:{minRows:1,maxRows:10}})})]})})]}),(0,e.jsx)(H.Z,{gutter:16,style:{marginTop:"16px"},children:(0,e.jsx)(B.Z,{span:12,children:(0,e.jsxs)(w.Z,{title:t.formatMessage({id:"pages.setting.discordSetting"}),bordered:!1,children:[(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoGetPrivateId"}),name:"enableAutoGetPrivateId",help:t.formatMessage({id:"pages.setting.enableAutoGetPrivateIdTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoVerifyAccount"}),name:"enableAutoVerifyAccount",help:t.formatMessage({id:"pages.setting.enableAutoVerifyAccountTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoSyncInfoSetting"}),name:"enableAutoSyncInfoSetting",help:t.formatMessage({id:"pages.setting.enableAutoSyncInfoSettingTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoExtendToken"}),name:"enableAutoExtendToken",help:t.formatMessage({id:"pages.setting.enableAutoExtendTokenTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableUserCustomUploadBase64"}),name:"enableUserCustomUploadBase64",help:t.formatMessage({id:"pages.setting.enableUserCustomUploadBase64Tips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveUserUploadBase64"}),name:"enableSaveUserUploadBase64",help:t.formatMessage({id:"pages.setting.enableSaveUserUploadBase64Tips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveUserUploadLink"}),name:"enableSaveUserUploadLink",help:t.formatMessage({id:"pages.setting.enableSaveUserUploadLinkTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveGeneratedImage"}),name:"enableSaveGeneratedImage",help:t.formatMessage({id:"pages.setting.enableSaveGeneratedImageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableSaveIntermediateImage"}),name:"enableSaveIntermediateImage",help:t.formatMessage({id:"pages.setting.enableSaveIntermediateImageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoDeleteImagineMessage"}),name:"enableAutoDeleteImagineMessage",help:t.formatMessage({id:"pages.setting.enableAutoDeleteImagineMessageTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertOfficialLink"}),name:"enableConvertOfficialLink",help:t.formatMessage({id:"pages.setting.enableConvertOfficialLinkTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableMjTranslate"}),name:"enableMjTranslate",help:t.formatMessage({id:"pages.setting.enableMjTranslateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableNijiTranslate"}),name:"enableNijiTranslate",help:t.formatMessage({id:"pages.setting.enableNijiTranslateTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertNijiToMj"}),name:"enableConvertNijiToMj",help:t.formatMessage({id:"pages.setting.enableConvertNijiToMjTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableConvertNijiToNijiBot"}),name:"enableConvertNijiToNijiBot",help:t.formatMessage({id:"pages.setting.enableConvertNijiToNijiBotTips"}),children:(0,e.jsx)(s.Z,{})}),(0,e.jsx)(n.Z.Item,{label:t.formatMessage({id:"pages.setting.enableAutoLogin"}),name:"enableAutoLogin",help:t.formatMessage({id:"pages.setting.enableAutoLoginTips"}),children:(0,e.jsx)(s.Z,{})})]})})})]})})})},pe=me}}]);
